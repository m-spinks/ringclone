using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using NHibernate;
using NHibernate.Criterion;
using System.Threading;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using FluentNHibernate.Mapping;
using System.Web.Script.Serialization;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using System.Runtime.Caching;
using System.Configuration;

namespace TicketProcessor
{
	public class Functions
	{
		// This function will get triggered/executed when a new message is written 
		// on an Azure Queue called queue.
		public static void ProcessQueueMessage([QueueTrigger("ticketprocessorqueue")] string message, TextWriter log)
		{
            log.WriteLine(message);
            var id = 0;
            if (int.TryParse(message, out id))
            {
                ExecuteBatch(id, log);
            }
		}

		public static void ExecuteBatch(int batchId, TextWriter log)
		{
            //foreach (var c in ConfigurationManager.ConnectionStrings)
            //{
            //    log.WriteLine(c.ToString().Replace("northtech@northtech", "****").Replace("123!@#qweQWE", "*****"));
            //}

            var model = new TicketProcessorModel();
            model.TransferBatchId = batchId;
            getBatchToProcess(model);
            foreach (var batch in model.BatchesToRun)
            {
                if (batch.RedoInd)
                    runRedo(batch, log);
                else
                    runBatch(batch, log);
            }
        }

        public static void ExecuteTicket(int ticketId, TextWriter log)
        {
            var model = new TicketProcessorModel();
            model.TransferBatchId = 0;
            Ticket ticket = null;
            bool ticketErr = false;
            using (ISessionFactory sessionFactory = CreateSessionFactory())
            {
                using (var session = sessionFactory.OpenStatelessSession())
                {
                    var ticketCrit = session.CreateCriteria<Ticket>();
                    ticketCrit.Add(Expression.Eq("TicketId", ticketId));
                    ticket = ticketCrit.UniqueResult<Ticket>();
                }
            }
            if (ticket != null)
            {
                doTransfer(null, ticket, ref ticketErr, log);
            }
        }

        public class TicketProcessorModel
		{
            public DateTime Now;
            public int TransferBatchId;
            public IList<TransferBatch> BatchesToRun;
            public IList<TransferBatch> RedosToRun;
            public IList<TicketLog> TicketLogs;
		}

        private static void getBatchToProcess(TicketProcessorModel model)
        {
            using (ISessionFactory sessionFactory = CreateSessionFactory())
            {
                using (var session = sessionFactory.OpenStatelessSession())
                {
                    var batchCrit = session.CreateCriteria<TransferBatch>();
                    batchCrit.Add(Expression.Eq("TransferBatchId", model.TransferBatchId));
                    model.BatchesToRun = batchCrit.List<TransferBatch>();
                }
            }
        }

        private static void getBatchesToRun(TicketProcessorModel model)
        {
            using (ISessionFactory sessionFactory = CreateSessionFactory())
            {
                using (var session = sessionFactory.OpenStatelessSession())
                {
                    var batchCrit = session.CreateCriteria<TransferBatch>();
                    batchCrit.Add(Expression.Eq("QueuedInd", true));
                    batchCrit.Add(Expression.Eq("RedoInd", false));
                    batchCrit.Add(Expression.Eq("ProcessingInd", false));
                    batchCrit.Add(Expression.Eq("DeletedInd", false));
                    model.BatchesToRun = batchCrit.List<TransferBatch>();
                }
            }
        }
        private static void getRedosToRun(TicketProcessorModel model)
        {
            using (ISessionFactory sessionFactory = CreateSessionFactory())
            {
                using (var session = sessionFactory.OpenStatelessSession())
                {
                    var batchCrit = session.CreateCriteria<TransferBatch>();
                    batchCrit.Add(Expression.Eq("QueuedInd", true));
                    batchCrit.Add(Expression.Eq("RedoInd", true));
                    batchCrit.Add(Expression.Eq("ProcessingInd", false));
                    batchCrit.Add(Expression.Eq("DeletedInd", false));
                    model.RedosToRun = batchCrit.List<TransferBatch>();
                }
            }
        }
        private static void runBatch(TransferBatch batch, TextWriter log)
        {
            TryNTimes(delegate
            {
                using (ISessionFactory sessionFactory = CreateSessionFactory())
                {
                    using (var session = sessionFactory.OpenStatelessSession())
                    {
                        using (var transaction = session.BeginTransaction())
                        {
                            batch.ProcessingInd = true;
                            batch.QueuedInd = false;
                            session.Update(batch);
                            transaction.Commit();
                        }
                        var ticketCrit = session.CreateCriteria<Ticket>();
                        ticketCrit.Add(Expression.Eq("TransferBatchId", batch.TransferBatchId));
                        ticketCrit.Add(Expression.Eq("DeletedInd", false));
                        ticketCrit.Add(Expression.IsNull("CompleteDate"));
                        batch.Tickets = ticketCrit.List<Ticket>();
                    }
                }
            }, 5);
            bool ticketErr = false;
            bool batchErr = false;
            int errorsInARow = 0;
            foreach (var ticket in batch.Tickets.Where(x => !x.CompleteDate.HasValue))
            {
                log.WriteLine(string.Format("TicketId: {0};", ticket.TicketId));
                doTransfer(batch, ticket, ref ticketErr, log);
                if (ticketErr)
                {
                    batchErr = true;
                    errorsInARow++;
                }
                else
                {
                    errorsInARow = 0;
                }
                if (errorsInARow >= 5)
                    break;
            }
            if (batch.Tickets.Any(x => !x.CompleteDate.HasValue))
            {
                ticketErr = false;
                batchErr = false;
                errorsInARow = 0;
                foreach (var ticket in batch.Tickets.Where(x => !x.CompleteDate.HasValue))
                {
                    log.WriteLine(string.Format("TicketId: {0};", ticket.TicketId));
                    doTransfer(batch, ticket, ref ticketErr, log);
                    if (ticketErr)
                    {
                        batchErr = true;
                        errorsInARow++;
                    }
                    else
                    {
                        errorsInARow = 0;
                    }
                    if (errorsInARow >= 5)
                        break;
                }
            }
            TryNTimes(delegate
            {
                batch.ProcessingInd = false;
                if (batchErr)
                {
                    batch.ErrorInd = true;
                }
                else
                {
                    batch.CompleteDate = DateTime.Now.ToUniversalTime();
                    batch.ErrorInd = false;
                }
                using (ISessionFactory sessionFactory = CreateSessionFactory())
                {
                    using (var session = sessionFactory.OpenStatelessSession())
                    {
                        using (var transaction = session.BeginTransaction())
                        {
                            session.Update(batch);
                            transaction.Commit();
                        }
                    }
                }
            }, 5);
        }
        private static void runRedo(TransferBatch batch, TextWriter log)
        {
            TryNTimes(delegate
            {
                using (ISessionFactory sessionFactory = CreateSessionFactory())
                {
                    using (var session = sessionFactory.OpenStatelessSession())
                    {
                        using (var transaction = session.BeginTransaction())
                        {
                            batch.ProcessingInd = true;
                            batch.QueuedInd = false;
                            session.Update(batch);
                            transaction.Commit();
                        }
                        var ticketCrit = session.CreateCriteria<Ticket>();
                        ticketCrit.Add(Expression.Eq("TransferBatchId", batch.TransferBatchId));
                        ticketCrit.Add(Expression.Eq("RedoInd", true));
                        ticketCrit.Add(Expression.Eq("DeletedInd", false));
                        ticketCrit.Add(Expression.IsNull("CompleteDate"));
                        batch.Tickets = ticketCrit.List<Ticket>();
                    }
                }
            }, 5);
            bool ticketErr = false;
            bool batchErr = false;
            foreach (var ticket in batch.Tickets)
            {
                log.WriteLine(string.Format("TicketId: {0};", ticket.TicketId));
                doTransfer(batch, ticket, ref ticketErr, log);
                if (ticketErr)
                    batchErr = true;
            }
            TryNTimes(delegate
            {
                using (ISessionFactory sessionFactory = CreateSessionFactory())
                {
                    using (var session = sessionFactory.OpenStatelessSession())
                    {
                        using (var transaction = session.BeginTransaction())
                        {
                            batch.ProcessingInd = false;
                            session.Update(batch);
                            transaction.Commit();
                        }
                    }
                }
            }, 5);
        }
        private static void doTransfer(TransferBatch batch, Ticket ticket, ref bool err, TextWriter log, bool enableDatabaseLogging = true)
        {
            err = true;
            var startTime = DateTime.Now.ToUniversalTime();
            var errMessage = "";
            TicketLog ticketLog = null;
            var archive = new Archive();
            archive.Files = new List<Archive.ArchiveFile>();
            archive.CallId = ticket.CallId;
            archive.MessageId = ticket.MessageId;
            archive.TicketId = ticket.TicketId;
            archive.Type = ticket.Type;
            archive.Files = new List<TicketProcessor.Functions.Archive.ArchiveFile>();
            TryNTimes(delegate
            {
                using (ISessionFactory sessionFactory = CreateSessionFactory())
                {
                    using (var session = sessionFactory.OpenStatelessSession())
                    {
                        using (var transaction = session.BeginTransaction())
                        {
                            ticket.ProcessingInd = true;
                            ticketLog = new TicketLog();
                            ticketLog.TicketId = ticket.TicketId;
                            ticketLog.TicketLogStartDate = DateTime.Now.ToUniversalTime();
                            ticketLog.TransferBatchId = ticket.TransferBatchId;
                            session.Update(ticket);
                            session.Insert(ticketLog);
                            transaction.Commit();
                        }
                    }
                }
            }, 5);
            // DO THE TRANSFER HERE
            var accountIdFromTicket = getAccountIdFromTicket(ticket);
            AccountRec account = (AccountRec)getFromCache("account" + batch.AccountId);
            AccountRec accountForDestination = (AccountRec)getFromCache("accountForDestination" + accountIdFromTicket);
            BoxAccountRec boxAccount = (BoxAccountRec)getFromCache("box" + ticket.DestinationBoxAccountId);
            GoogleAccountRec googleAccount = (GoogleAccountRec)getFromCache("google" + ticket.DestinationGoogleAccountId);
            AmazonAccountRec amazonAccount = (AmazonAccountRec)getFromCache("amazon" + ticket.DestinationAmazonAccountId);
            var thingsToSave = new List<ThingToSave>();
            TicketRawDataRec rawdataRec = null;
            string recordId = "";
            try
            {
                archive.TicketLogId = ticketLog.TicketLogId;
                if (account == null || accountForDestination == null)
                {
                    LogWriteLine("Looking up your RingCentral credentials", LogTextStatus.None, log, ticketLog, enableDatabaseLogging);
                    using (ISessionFactory sessionFactory = CreateSessionFactory())
                    {
                        using (var session = sessionFactory.OpenSession())
                        {
                            var accountCrit = session.CreateCriteria<AccountRec>();
                            accountCrit.Add(Expression.Eq("AccountId", batch.AccountId));
                            var accounts = accountCrit.List<AccountRec>();
                            if (accounts.Any())
                            {
                                account = accounts.First();
                                saveToCache("account" + account.AccountId, account);
                                LogWriteLine("RingCentral credentials found", LogTextStatus.None, log, ticketLog, enableDatabaseLogging);
                            }
                            else
                            {
                                LogWriteLine("RingCentral credentials not found", LogTextStatus.Danger, log, ticketLog, enableDatabaseLogging);
                                errMessage = "RingCentral credentials not found";
                            }
                            var accountForDestinationCrit = session.CreateCriteria<AccountRec>();
                            accountForDestinationCrit.Add(Expression.Eq("AccountId", accountIdFromTicket));
                            var accountsForDestination = accountForDestinationCrit.List<AccountRec>();
                            if (accountsForDestination.Any())
                            {
                                accountForDestination = accountsForDestination.First();
                                saveToCache("accountForDestination" + accountsForDestination.First().AccountId, accountsForDestination.First());
                                LogWriteLine("RingCentral credentials for destination found", LogTextStatus.None, log, ticketLog, enableDatabaseLogging);
                            }
                            else
                            {
                                LogWriteLine("RingCentral credentials for destination not found", LogTextStatus.Danger, log, ticketLog, enableDatabaseLogging);
                                errMessage = "RingCentral credentials for destination not found";
                            }
                        }
                    }
                }
                LogWriteLine("Extracting RingCentral record from database", LogTextStatus.None, log, ticketLog, enableDatabaseLogging);
                using (ISessionFactory sessionFactory = CreateSessionFactory())
                {
                    using (var session = sessionFactory.OpenSession())
                    {
                        var rawdataCriteria = session.CreateCriteria<TicketRawDataRec>();
                        rawdataCriteria.Add(Expression.Eq("TicketId", ticket.TicketId));
                        var rawdatas = rawdataCriteria.List<TicketRawDataRec>();
                        if (rawdatas.Any())
                        {
                            rawdataRec = rawdatas.First();
                            LogWriteLine("RingCentral record extracted from database", LogTextStatus.None, log, ticketLog, enableDatabaseLogging);
                        }
                        else
                        {
                            LogWriteLine("RingCentral record not found", LogTextStatus.Danger, log, ticketLog, enableDatabaseLogging);
                            errMessage = "RingCentral record not found";
                        }
                    }
                }
                if (account != null && accountForDestination != null && rawdataRec != null)
                {
                    archive.Owner = account.RingCentralOwnerId;
                    if (ticket.Type == "fax" || ticket.Type == "sms")
                    {
                        LogWriteLine("De-serializing RingCentral message store record", LogTextStatus.None, log, ticketLog, enableDatabaseLogging);
                        var messageStoreRec = JsonConvert.DeserializeObject<RingCentral.MessageStore.MessageStoreData.Record>(rawdataRec.RawData);
                        if (messageStoreRec == null)
                        {
                            LogWriteLine("Unable to de-serialize RingCentral message store record", LogTextStatus.Danger, log, ticketLog, enableDatabaseLogging);
                            errMessage = "Unable to de-serialize RingCentral message store record";
                        }
                        else
                        {
                            LogWriteLine("RingCentral message store record de-serialized", LogTextStatus.None, log, ticketLog, enableDatabaseLogging);
                            DateTime tDate;
                            var timestamp = (DateTime.TryParse(messageStoreRec.creationTime, out tDate) ? tDate.ToUniversalTime() : DateTime.Now.ToUniversalTime());
                            archive.MessageLog = messageStoreRec;
                            archive.CallTime = timestamp;
                            recordId = messageStoreRec.id;
                            ticket.CallTime = timestamp;
                            if (ticket.ContentInd)
                                LogWriteLine("Analyzing recepients and attachments", LogTextStatus.None, log, ticketLog, enableDatabaseLogging);
                            var totalAttachments = 0;
                            var attachmentList = new List<RingCentral.MessageStore.MessageStoreData.MessageAttachmentInfo>();
                            if (messageStoreRec.attachments != null)
                            {
                                foreach (var attachment in messageStoreRec.attachments)
                                {
                                    if (attachment.id != null && !attachmentList.Any(x => x.id == attachment.id))
                                    {
                                        totalAttachments++;
                                        attachmentList.Add(attachment);
                                    }
                                }
                            }
                            if (ticket.ContentInd)
                                LogWriteLine(totalAttachments + " attachment(s) found", LogTextStatus.None, log, ticketLog, enableDatabaseLogging);
                            LogWriteLine("Generating file names", LogTextStatus.None, log, ticketLog, enableDatabaseLogging);
                            var coreFileName = generateCoreFileName(messageStoreRec);
                            handleNameCollisions(ticket);
                            if (ticket.NameIteration > 1)
                                coreFileName += "." + ticket.NameIteration.ToString();
                            archive.DefaultFileName = coreFileName;
                            if (ticket.LogInd)
                            {
                                //MAIN LOG FILE
                                thingsToSave.Add(new ThingToSave()
                                {
                                    Id = messageStoreRec.id,
                                    Timestamp = timestamp,
                                    ContentType = "text/plain",
                                    ContentUri = "",
                                    RawData = JsonConvert.SerializeObject(messageStoreRec, Formatting.Indented),
                                    Filename = coreFileName + ".log"
                                });
                            }
                            if (ticket.ContentInd)
                            {
                                //ATTACHMENT(S)
                                var attachmentCounter = 1;
                                foreach (var attachment in attachmentList)
                                {
                                    thingsToSave.Add(new ThingToSave()
                                    {
                                        Id = attachment.id,
                                        Timestamp = timestamp,
                                        ContentType = attachment.contentType,
                                        ContentUri = attachment.uri,
                                        RawData = "",
                                        Filename = coreFileName + "_" + attachment.type + (attachmentList.Count > 1 ? "-" + attachmentCounter.ToString("00") : "") + "." + getFileExtension(attachment.contentType)
                                    });
                                    attachmentCounter++;
                                }
                            }
                        }
                    }
                    else //(voice)
                    {
                        LogWriteLine("De-serializing RingCentral call log record", LogTextStatus.None, log, ticketLog, enableDatabaseLogging);
                        var callLogRec = JsonConvert.DeserializeObject<RingCentral.CallLog.CallLogData.Record>(rawdataRec.RawData);
                        if (callLogRec == null)
                        {
                            LogWriteLine("Unable to de-serialize RingCentral call log record", LogTextStatus.Danger, log, ticketLog, enableDatabaseLogging);
                            errMessage = "Unable to de-serialize RingCentral call log record";
                        }
                        else
                        {
                            LogWriteLine("RingCentral call log record de-serialized", LogTextStatus.None, log, ticketLog, enableDatabaseLogging);
                            DateTime tDate;
                            var timestamp = (DateTime.TryParse(callLogRec.startTime, out tDate) ? tDate.ToUniversalTime() : DateTime.Now.ToUniversalTime());
                            archive.CallLog = callLogRec;
                            archive.CallTime = timestamp;
                            recordId = callLogRec.id;
                            ticket.CallTime = timestamp;
                            if (ticket.ContentInd)
                                LogWriteLine("Analyzing voicemails and recordings", LogTextStatus.None, log, ticketLog, enableDatabaseLogging);
                            var totalVoicemails = 0;
                            var totalRecordings = 0;
                            var voicemailList = new List<KeyValuePair<int, RingCentral.CallLog.CallLogData.VoicemailMessageInfo>>();
                            var voicemailAttachmentList = new List<KeyValuePair<int, RingCentral.Message.MessageData.MessageAttachmentInfo>>();
                            var recordingList = new List<KeyValuePair<int, RingCentral.CallLog.CallLogData.RecordingInfo>>();
                            var legCounter = 1;
                            if (callLogRec.message != null && callLogRec.message.id != null)
                            {
                                totalVoicemails++;
                                voicemailList.Add(new KeyValuePair<int, RingCentral.CallLog.CallLogData.VoicemailMessageInfo>(0, callLogRec.message));
                            }
                            if (callLogRec.recording != null && callLogRec.recording.id != null)
                            {
                                totalRecordings++;
                                recordingList.Add(new KeyValuePair<int, RingCentral.CallLog.CallLogData.RecordingInfo>(0, callLogRec.recording));
                            }
                            if (callLogRec.legs != null)
                            {
                                foreach (var leg in callLogRec.legs)
                                {
                                    if (leg.message != null && leg.message.id != null && !voicemailList.Any(x => x.Value.id == leg.message.id))
                                    {
                                        totalVoicemails++;
                                        voicemailList.Add(new KeyValuePair<int, RingCentral.CallLog.CallLogData.VoicemailMessageInfo>(legCounter, leg.message));
                                    }
                                    if (leg.recording != null && leg.recording.uri != null && !recordingList.Any(x => x.Value.id == leg.recording.id))
                                    {
                                        totalRecordings++;
                                        recordingList.Add(new KeyValuePair<int, RingCentral.CallLog.CallLogData.RecordingInfo>(legCounter, leg.recording));
                                    }
                                    legCounter++;
                                }
                            }
                            if (ticket.ContentInd)
                            {
                                LogWriteLine(totalVoicemails + " voicemail(s) and " + totalRecordings + " recording(s) found", LogTextStatus.None, log, ticketLog, enableDatabaseLogging);
                                if (voicemailList.Any())
                                {
                                    LogWriteLine("Analyzing voicemail attachment(s)", LogTextStatus.None, log, ticketLog, enableDatabaseLogging);
                                    foreach (var vm in voicemailList)
                                    {
                                        var mStore = new RingCentral.Message(account.RingCentralId).MessageUrl(vm.Value.uri);
                                        mStore.Execute();
                                        if (mStore.data != null && mStore.data.attachments != null)
                                        {
                                            var voicemailAttachmentIndex = 0;
                                            foreach (var attachment in mStore.data.attachments)
                                            {
                                                voicemailAttachmentList.Add(new KeyValuePair<int, RingCentral.Message.MessageData.MessageAttachmentInfo>(voicemailAttachmentIndex, attachment));
                                                voicemailAttachmentIndex++;
                                            }
                                        }
                                    }
                                }
                            }
                            LogWriteLine("Generating file name(s)", LogTextStatus.None, log, ticketLog, enableDatabaseLogging);
                            var coreFileName = generateCoreFileName(callLogRec);
                            handleNameCollisions(ticket);
                            if (ticket.NameIteration > 1)
                                coreFileName += "." + ticket.NameIteration.ToString();
                            archive.DefaultFileName = coreFileName;
                            if (ticket.LogInd)
                            {
                                //MAIN LOG FILE
                                thingsToSave.Add(new ThingToSave()
                                {
                                    Id = callLogRec.id,
                                    Timestamp = timestamp,
                                    ContentType = "text/plain",
                                    ContentUri = "",
                                    RawData = JsonConvert.SerializeObject(callLogRec, Formatting.Indented),
                                    Filename = coreFileName + ".log"
                                });
                            }
                            if (ticket.ContentInd)
                            {
                                //VOICEMAIL MESSAGE(S)
                                foreach (var voicemailAttachment in voicemailAttachmentList)
                                {
                                    thingsToSave.Add(new ThingToSave()
                                    {
                                        Id = voicemailAttachment.Value.id,
                                        Timestamp = timestamp,
                                        ContentType = voicemailAttachment.Value.contentType,
                                        ContentUri = voicemailAttachment.Value.uri,
                                        RawData = "",
                                        Filename = coreFileName + "_Message" + (voicemailAttachmentList.Count > 1 && voicemailAttachment.Key > 0 ? "-" + voicemailAttachment.Key : "") + "." + getFileExtension(voicemailAttachment.Value.type, "mp3")
                                    });
                                }
                                //CALL RECORDING(S)
                                var recordingCounter = 1;
                                foreach (var recording in recordingList)
                                {
                                    thingsToSave.Add(new ThingToSave()
                                    {
                                        Id = recording.Value.id,
                                        Timestamp = timestamp,
                                        ContentType = recording.Value.type,
                                        ContentUri = recording.Value.contentUri,
                                        RawData = "",
                                        Filename = coreFileName + (recordingList.Count > 1 && recording.Key > 0 ? "_Leg-" + recording.Key : "") + "_Recording" + "." + getFileExtension(recording.Value.type, "mp3")
                                    });
                                    recordingCounter++;
                                }
                            }
                        }
                    }
                }
                if (account != null && accountForDestination != null && rawdataRec != null)
                {
                    if (thingsToSave == null || !thingsToSave.Any())
                    {
                        //NOTHING TO SAVE. JUST SET THE ERROR TO FALSE.
                        LogWriteLine("Nothing to archive for " + recordId, LogTextStatus.None, log, ticketLog, enableDatabaseLogging);
                        err = false;
                    }
                    else
                    {
                        if (ticket.Destination == "box")
                        {
                            if (boxAccount == null)
                            {
                                LogWriteLine("Looking up your Box credentials", LogTextStatus.None, log, ticketLog, enableDatabaseLogging);
                                using (ISessionFactory sessionFactory = CreateSessionFactory())
                                {
                                    using (var session = sessionFactory.OpenSession())
                                    {
                                        var boxAccountCrit = session.CreateCriteria<BoxAccountRec>();
                                        boxAccountCrit.Add(Expression.Eq("AccountId", accountForDestination.AccountId));
                                        boxAccountCrit.Add(Expression.Eq("BoxAccountId", ticket.DestinationBoxAccountId));
                                        var boxAccounts = boxAccountCrit.List<BoxAccountRec>();
                                        if (boxAccounts.Any())
                                        {
                                            boxAccount = boxAccounts.First();
                                            saveToCache("box" + boxAccount.BoxAccountId, boxAccount);
                                            LogWriteLine("Box credentials found", LogTextStatus.None, log, ticketLog, enableDatabaseLogging);
                                        }
                                        else
                                        {
                                            LogWriteLine("Box credentials not found", LogTextStatus.Danger, log, ticketLog, enableDatabaseLogging);
                                            errMessage = "Box credentials not found";
                                        }
                                    }
                                }
                            }
                            if (boxAccount != null)
                            {
                                var successCount = 0;
                                foreach (var thingToSave in thingsToSave)
                                {
                                    if (!string.IsNullOrWhiteSpace(thingToSave.ContentUri))
                                    {
                                        LogWriteLine("Retrieving content from RingCentral for " + thingToSave.Id, LogTextStatus.None, log, ticketLog, enableDatabaseLogging);
                                        var contentDownloader = new RingCentral.ContentDownloader(account.RingCentralId, thingToSave.ContentUri);
                                        contentDownloader.Execute();
                                        if (contentDownloader.data != null && contentDownloader.data.Length > 0)
                                        {
                                            LogWriteLine("Content retrieved for " + thingToSave.Id + " (" + contentDownloader.data.Length + " bytes found)", LogTextStatus.None, log, ticketLog, enableDatabaseLogging);
                                            LogWriteLine("Transferring content to Box for " + recordId, LogTextStatus.None, log, ticketLog, enableDatabaseLogging);
                                            var boxUploader = new Box.BoxUpload(accountForDestination.RingCentralId, ticket.DestinationBoxAccountId);
                                            if (ticket.PutInDatedSubfolder)
                                                boxUploader.Folder(ticket.DestinationFolderId).NavigateToOrCreateSubFolder(thingToSave.Timestamp.ToString("yyyyMM")).FileName(thingToSave.Filename).FileData(contentDownloader.data);
                                            else
                                                boxUploader.Folder(ticket.DestinationFolderId).FileName(thingToSave.Filename).FileData(contentDownloader.data);
                                            boxUploader.Execute();
                                            if (boxUploader.ResultException == null && boxUploader.Response != null && boxUploader.Response.entries != null && boxUploader.Response.entries.Count > 0)
                                            {
                                                LogWriteLine("Content transferred to Box for " + thingToSave.Id, LogTextStatus.Success, log, ticketLog, enableDatabaseLogging);
                                                successCount++;
                                                var uploadedFile = boxUploader.Response.entries.First();
                                                archive.Files.Add(new Archive.ArchiveFile()
                                                {
                                                    FileId = uploadedFile.id,
                                                    ContentInd = true,
                                                    DestinationAccountId = ticket.DestinationBoxAccountId,
                                                    Destination = "box",
                                                    Filename = uploadedFile.name,
                                                    Folder = ticket.DestinationFolderLabel,
                                                    LogInd = false,
                                                    NumberOfBytes = contentDownloader.data.Length
                                                });
                                            }
                                            else
                                            {
                                                LogWriteLine("Unable to transfer content to Box for " + thingToSave.Id, LogTextStatus.Danger, log, ticketLog, enableDatabaseLogging);
                                                errMessage = "Unable to transfer recording to Box for " + thingToSave.Id + " - " + boxUploader.ResultException.Message;
                                                if (boxUploader.ResultException != null)
                                                    errMessage = "Unable to transfer recording to Box for " + thingToSave.Id + " - " + boxUploader.ResultException.Message;
                                                else
                                                    errMessage = "Unable to transfer recording to Box for " + thingToSave.Id;
                                            }
                                        }
                                        else
                                        {
                                            LogWriteLine("Unable to retrieve content for " + thingToSave.Id, LogTextStatus.Danger, log, ticketLog, enableDatabaseLogging);
                                            if (contentDownloader.ResultException != null)
                                                errMessage = "Unable to retrieve content for " + thingToSave.Id + " - " + contentDownloader.ResultException.Message;
                                            else
                                                errMessage = "Unable to retrieve content for " + thingToSave.Id;
                                            addTraceForLog(ticketLog.TicketLogId, ticket.TicketId, ticket.TransferBatchId, contentDownloader.headers);
                                        }
                                    }
                                    else
                                    {
                                        byte[] bytes = Encoding.ASCII.GetBytes(thingToSave.RawData);
                                        LogWriteLine("Transferring " + thingToSave.Id + " log to Box", LogTextStatus.None, log, ticketLog, enableDatabaseLogging);
                                        var boxUploader = new Box.BoxUpload(accountForDestination.RingCentralId, ticket.DestinationBoxAccountId);
                                        if (ticket.PutInDatedSubfolder)
                                            boxUploader.Folder(ticket.DestinationFolderId).NavigateToOrCreateSubFolder(thingToSave.Timestamp.ToString("yyyyMM")).FileName(thingToSave.Filename).FileData(bytes);
                                        else
                                            boxUploader.Folder(ticket.DestinationFolderId).FileName(thingToSave.Filename).FileData(bytes);
                                        boxUploader.Execute();
                                        if (boxUploader.ResultException == null && boxUploader.Response != null && boxUploader.Response.entries != null && boxUploader.Response.entries.Count > 0)
                                        {
                                            LogWriteLine("Transfer of " + thingToSave.Id + " log to Box succeeded", LogTextStatus.Success, log, ticketLog, enableDatabaseLogging);
                                            successCount++;
                                            var uploadedFile = boxUploader.Response.entries.First();
                                            archive.Files.Add(new Archive.ArchiveFile()
                                            {
                                                FileId = uploadedFile.id,
                                                ContentInd = false,
                                                DestinationAccountId = ticket.DestinationBoxAccountId,
                                                Destination = "box",
                                                Filename = uploadedFile.name,
                                                Folder = ticket.DestinationFolderLabel,
                                                LogInd = true,
                                                NumberOfBytes = bytes.Length
                                            });
                                        }
                                        else
                                        {
                                            LogWriteLine("Unable to transfer " + thingToSave.Id + " log to Box", LogTextStatus.Danger, log, ticketLog, enableDatabaseLogging);
                                            errMessage = "Unable to transfer " + thingToSave.Id + " log to Box - " + boxUploader.ResultException.Message;
                                            if (boxUploader.ResultException != null)
                                                errMessage = "Unable to transfer " + thingToSave.Id + " log to Box - " + boxUploader.ResultException.Message;
                                            else
                                                errMessage = "Unable to transfer " + thingToSave.Id + " log to Box";
                                        }
                                    }
                                }
                                err = successCount != thingsToSave.Count;
                            }
                        }
                        else if (ticket.Destination == "google")
                        {
                            if (googleAccount == null)
                            {
                                LogWriteLine("Looking up your Google credentials", LogTextStatus.None, log, ticketLog, enableDatabaseLogging);
                                using (ISessionFactory sessionFactory = CreateSessionFactory())
                                {
                                    using (var session = sessionFactory.OpenSession())
                                    {
                                        var googleAccountCrit = session.CreateCriteria<GoogleAccountRec>();
                                        googleAccountCrit.Add(Expression.Eq("AccountId", accountForDestination.AccountId));
                                        googleAccountCrit.Add(Expression.Eq("GoogleAccountId", ticket.DestinationGoogleAccountId));
                                        var googleAccounts = googleAccountCrit.List<GoogleAccountRec>();
                                        if (googleAccounts.Any())
                                        {
                                            googleAccount = googleAccounts.First();
                                            saveToCache("google" + googleAccount.GoogleAccountId, googleAccount);
                                            LogWriteLine("Google credentials found", LogTextStatus.None, log, ticketLog, enableDatabaseLogging);
                                        }
                                        else
                                        {
                                            LogWriteLine("Google credentials not found", LogTextStatus.Danger, log, ticketLog, enableDatabaseLogging);
                                            errMessage = "Google credentials not found";
                                        }
                                    }
                                }
                            }
                            if (googleAccount != null)
                            {
                                var successCount = 0;
                                foreach (var thingToSave in thingsToSave)
                                {
                                    if (!string.IsNullOrWhiteSpace(thingToSave.ContentUri))
                                    {
                                        LogWriteLine("Retrieving content from RingCentral for " + thingToSave.Id, LogTextStatus.None, log, ticketLog, enableDatabaseLogging);
                                        var contentDownloader = new RingCentral.ContentDownloader(account.RingCentralId, thingToSave.ContentUri);
                                        contentDownloader.Execute();
                                        if (contentDownloader.data != null && contentDownloader.data.Length > 0)
                                        {
                                            LogWriteLine("Content retrieved for " + thingToSave.Id + " (" + contentDownloader.data.Length + " bytes found)", LogTextStatus.None, log, ticketLog, enableDatabaseLogging);
                                            LogWriteLine("Transferring content to Google Drive for " + thingToSave.Id, LogTextStatus.None, log, ticketLog, enableDatabaseLogging);
                                            var googleUploader = new GoogleActions.GoogleUpload(accountForDestination.RingCentralId, ticket.DestinationGoogleAccountId);
                                            if (ticket.PutInDatedSubfolder)
                                                googleUploader.Folder(ticket.DestinationFolderId).NavigateToOrCreateSubFolder(thingToSave.Timestamp.ToString("yyyyMM")).FileName(thingToSave.Filename).FileData(contentDownloader.data);
                                            else
                                                googleUploader.Folder(ticket.DestinationFolderId).FileName(thingToSave.Filename).FileData(contentDownloader.data);
                                            googleUploader.Execute();
                                            if (googleUploader.ResultException == null && googleUploader.Response != null && !string.IsNullOrEmpty(googleUploader.Response.id))
                                            {
                                                LogWriteLine("Content transferred to Google Drive for " + thingToSave.Id, LogTextStatus.Success, log, ticketLog, enableDatabaseLogging);
                                                successCount++;
                                                var uploadedFile = googleUploader.Response;
                                                archive.Files.Add(new Archive.ArchiveFile()
                                                {
                                                    FileId = uploadedFile.id,
                                                    ContentInd = true,
                                                    DestinationAccountId = ticket.DestinationGoogleAccountId,
                                                    Destination = "google",
                                                    Filename = uploadedFile.name,
                                                    Folder = ticket.DestinationFolderLabel,
                                                    LogInd = false,
                                                    NumberOfBytes = contentDownloader.data.Length
                                                });
                                            }
                                            else
                                            {
                                                LogWriteLine("Unable to transfer content to Google Drive for " + thingToSave.Id, LogTextStatus.Danger, log, ticketLog, enableDatabaseLogging);
                                                errMessage = "Unable to transfer recording to Google Drive for " + thingToSave.Id + " - " + googleUploader.ResultException.Message;
                                                if (googleUploader.ResultException != null)
                                                    errMessage = "Unable to transfer recording to Google Drive for " + thingToSave.Id + " - " + googleUploader.ResultException.Message;
                                                else
                                                    errMessage = "Unable to transfer recording to Google Drive for " + thingToSave.Id;
                                            }
                                        }
                                        else
                                        {
                                            LogWriteLine("Unable to retrieve content for " + thingToSave.Id, LogTextStatus.Danger, log, ticketLog, enableDatabaseLogging);
                                            if (contentDownloader.ResultException != null)
                                                errMessage = "Unable to retrieve content for " + thingToSave.Id + " - " + contentDownloader.ResultException.Message;
                                            else
                                                errMessage = "Unable to retrieve content for " + thingToSave.Id;
                                            addTraceForLog(ticketLog.TicketLogId, ticket.TicketId, ticket.TransferBatchId, contentDownloader.headers);
                                        }
                                    }
                                    else
                                    {
                                        byte[] bytes = Encoding.ASCII.GetBytes(thingToSave.RawData);
                                        LogWriteLine("Transferring " + thingToSave.Id + " log to Google Drive", LogTextStatus.None, log, ticketLog, enableDatabaseLogging);
                                        var googleUploader = new GoogleActions.GoogleUpload(accountForDestination.RingCentralId, ticket.DestinationGoogleAccountId);
                                        if (ticket.PutInDatedSubfolder)
                                            googleUploader.Folder(ticket.DestinationFolderId).NavigateToOrCreateSubFolder(thingToSave.Timestamp.ToString("yyyyMM")).FileName(thingToSave.Filename).FileData(bytes);
                                        else
                                            googleUploader.Folder(ticket.DestinationFolderId).FileName(thingToSave.Filename).FileData(bytes);
                                        googleUploader.Execute();
                                        if (googleUploader.ResultException == null && googleUploader.Response != null && !string.IsNullOrEmpty(googleUploader.Response.id))
                                        {
                                            LogWriteLine("Transfer of " + thingToSave.Id + " log to Google Drive succeeded", LogTextStatus.Success, log, ticketLog, enableDatabaseLogging);
                                            successCount++;
                                            var uploadedFile = googleUploader.Response;
                                            archive.Files.Add(new Archive.ArchiveFile()
                                            {
                                                FileId = uploadedFile.id,
                                                ContentInd = false,
                                                DestinationAccountId = ticket.DestinationGoogleAccountId,
                                                Destination = "google",
                                                Filename = uploadedFile.name,
                                                Folder = ticket.DestinationFolderLabel,
                                                LogInd = true,
                                                NumberOfBytes = bytes.Length
                                            });
                                        }
                                        else
                                        {
                                            LogWriteLine("Unable to transfer " + thingToSave.Id + " log to Google Drive", LogTextStatus.Danger, log, ticketLog, enableDatabaseLogging);
                                            errMessage = "Unable to transfer " + thingToSave.Id + " log to Google Drive - " + googleUploader.ResultException.Message;
                                            if (googleUploader.ResultException != null)
                                                errMessage = "Unable to transfer " + thingToSave.Id + " log to Google Drive - " + googleUploader.ResultException.Message;
                                            else
                                                errMessage = "Unable to transfer " + thingToSave.Id + " log to Google Drive";
                                        }
                                    }
                                }
                                err = successCount != thingsToSave.Count;
                            }
                        }
                        else if (ticket.Destination == "amazon")
                        {
                            if (amazonAccount == null)
                            {
                                LogWriteLine("Looking up your Amazon credentials", LogTextStatus.None, log, ticketLog, enableDatabaseLogging);
                                using (ISessionFactory sessionFactory = CreateSessionFactory())
                                {
                                    using (var session = sessionFactory.OpenSession())
                                    {
                                        var amazonAccountCrit = session.CreateCriteria<AmazonAccountRec>();
                                        amazonAccountCrit.Add(Expression.Eq("AccountId", accountForDestination.AccountId));
                                        amazonAccountCrit.Add(Expression.Eq("AmazonAccountId", ticket.DestinationAmazonAccountId));
                                        var amazonAccounts = amazonAccountCrit.List<AmazonAccountRec>();
                                        if (amazonAccounts.Any())
                                        {
                                            amazonAccount = amazonAccounts.First();
                                            saveToCache("amazon" + amazonAccount.AmazonAccountId, amazonAccount);
                                            LogWriteLine("Amazon credentials found", LogTextStatus.None, log, ticketLog, enableDatabaseLogging);
                                        }
                                        else
                                        {
                                            LogWriteLine("Amazon credentials not found", LogTextStatus.Danger, log, ticketLog, enableDatabaseLogging);
                                            errMessage = "Amazon credentials not found";
                                        }
                                    }
                                }
                            }
                            if (amazonAccount != null)
                            {
                                var successCount = 0;
                                foreach (var thingToSave in thingsToSave)
                                {
                                    if (!string.IsNullOrWhiteSpace(thingToSave.ContentUri))
                                    {
                                        LogWriteLine("Retrieving content from RingCentral for " + thingToSave.Id, LogTextStatus.None, log, ticketLog, enableDatabaseLogging);
                                        var contentDownloader = new RingCentral.ContentDownloader(account.RingCentralId, thingToSave.ContentUri);
                                        contentDownloader.Execute();
                                        if (contentDownloader.data != null && contentDownloader.data.Length > 0)
                                        {
                                            LogWriteLine("Content retrieved for " + thingToSave.Id + " (" + contentDownloader.data.Length + " bytes found)", LogTextStatus.None, log, ticketLog, enableDatabaseLogging);
                                            LogWriteLine("Transferring content to Amazon bucket for " + thingToSave.Id, LogTextStatus.None, log, ticketLog, enableDatabaseLogging);
                                            var amazonUploader = new AmazonActions.AmazonUpload(accountForDestination.RingCentralId, ticket.DestinationAmazonAccountId);
                                            var prefix = "";
                                            if (ticket.PutInDatedSubfolder)
                                                prefix = ticket.DestinationPrefix + thingToSave.Timestamp.ToString("yyyyMM") + "/";
                                            else
                                                prefix = ticket.DestinationPrefix;
                                            amazonUploader.BucketName(ticket.DestinationBucketName).Prefix(prefix).FileName(thingToSave.Filename).FileData(contentDownloader.data);
                                            amazonUploader.Execute();
                                            if (amazonUploader.ResultException == null)
                                            {
                                                LogWriteLine("Content transferred to Amazon bucket for " + thingToSave.Id, LogTextStatus.Success, log, ticketLog, enableDatabaseLogging);
                                                successCount++;
                                                archive.Files.Add(new Archive.ArchiveFile()
                                                {
                                                    BucketName = ticket.DestinationBucketName,
                                                    ContentInd = true,
                                                    DestinationAccountId = ticket.DestinationAmazonAccountId,
                                                    Destination = "amazon",
                                                    Filename = thingToSave.Filename,
                                                    LogInd = false,
                                                    NumberOfBytes = contentDownloader.data.Length,
                                                    Prefix = ticket.DestinationPrefix
                                                });
                                            }
                                            else
                                            {
                                                LogWriteLine("Unable to transfer content to Amazon bucket for " + thingToSave.Id, LogTextStatus.Danger, log, ticketLog, enableDatabaseLogging);
                                                errMessage = "Unable to transfer recording to Amazon bucket for " + thingToSave.Id + " - " + amazonUploader.ResultException.Message;
                                                if (amazonUploader.ResultException != null)
                                                    errMessage = "Unable to transfer recording to Amazon bucket for " + thingToSave.Id + " - " + amazonUploader.ResultException.Message;
                                                else
                                                    errMessage = "Unable to transfer recording to Amazon bucket for " + thingToSave.Id;
                                            }
                                        }
                                        else
                                        {
                                            LogWriteLine("Unable to retrieve content for " + thingToSave.Id, LogTextStatus.Danger, log, ticketLog, enableDatabaseLogging);
                                            if (contentDownloader.ResultException != null)
                                                errMessage = "Unable to retrieve content for " + thingToSave.Id + " - " + contentDownloader.ResultException.Message;
                                            else
                                                errMessage = "Unable to retrieve content for " + thingToSave.Id;
                                            addTraceForLog(ticketLog.TicketLogId, ticket.TicketId, ticket.TransferBatchId, contentDownloader.headers);
                                        }
                                    }
                                    else
                                    {
                                        byte[] bytes = Encoding.ASCII.GetBytes(thingToSave.RawData);
                                        LogWriteLine("Transferring " + thingToSave.Id + " log to Amazon bucket", LogTextStatus.None, log, ticketLog, enableDatabaseLogging);
                                        var amazonUploader = new AmazonActions.AmazonUpload(accountForDestination.RingCentralId, ticket.DestinationAmazonAccountId);
                                        var prefix = "";
                                        if (ticket.PutInDatedSubfolder)
                                            prefix = ticket.DestinationPrefix + thingToSave.Timestamp.ToString("yyyyMM") + "/";
                                        else
                                            prefix = ticket.DestinationPrefix;
                                        amazonUploader.BucketName(ticket.DestinationBucketName).Prefix(prefix).FileName(thingToSave.Filename).FileData(bytes);
                                        amazonUploader.Execute();
                                        if (amazonUploader.ResultException == null)
                                        {
                                            LogWriteLine("Transfer of " + thingToSave.Id + " log to Amazon bucket succeeded", LogTextStatus.Success, log, ticketLog, enableDatabaseLogging);
                                            successCount++;
                                            archive.Files.Add(new Archive.ArchiveFile()
                                            {
                                                BucketName = ticket.DestinationBucketName,
                                                ContentInd = false,
                                                DestinationAccountId = ticket.DestinationAmazonAccountId,
                                                Destination = "amazon",
                                                Filename = thingToSave.Filename,
                                                LogInd = true,
                                                NumberOfBytes = bytes.Length,
                                                Prefix = ticket.DestinationPrefix
                                            });
                                        }
                                        else
                                        {
                                            LogWriteLine("Unable to transfer " + thingToSave.Id + " log to Amazon bucket", LogTextStatus.Danger, log, ticketLog, enableDatabaseLogging);
                                            errMessage = "Unable to transfer " + thingToSave.Id + " log to Amazon bucket - " + amazonUploader.ResultException.Message;
                                            if (amazonUploader.ResultException != null)
                                                errMessage = "Unable to transfer " + thingToSave.Id + " log to Amazon bucket - " + amazonUploader.ResultException.Message;
                                            else
                                                errMessage = "Unable to transfer " + thingToSave.Id + " log to Amazon bucket";
                                        }
                                    }
                                }
                                err = successCount != thingsToSave.Count;
                            }
                        }
                        else
                        {
                            LogWriteLine("RingCentral credentials failed validation", LogTextStatus.Danger, log, ticketLog, enableDatabaseLogging);
                            errMessage = "RingCentral credentials failed validation";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    errMessage += " ---> " + ex.InnerException.Message;
                    if (ex.InnerException.InnerException != null)
                    {
                        errMessage += " ---> " + ex.InnerException.InnerException.Message;
                    }
                }
            }
            //FINAL LOGGING AND FLAGGING
            if (err)
            {
                ticketLog.ErrorInd = true;
                ticket.ErrorInd = true;
            }
            else
            {
                archive.TimeElapsedForTransfer = (int)Math.Floor((DateTime.Now.ToUniversalTime() - startTime).TotalMilliseconds);
                AddToIndex(archive);
                ticket.CompleteDate = DateTime.Now.ToUniversalTime();
                ticket.ErrorInd = false;
            }
            if (!string.IsNullOrWhiteSpace(errMessage))
                ticketLog.Message = errMessage;
            ticket.ProcessingInd = false;
            ticket.RedoInd = false;
            TryNTimes(delegate
            {
                using (ISessionFactory sessionFactory = CreateSessionFactory())
                {
                    using (var session = sessionFactory.OpenStatelessSession())
                    {
                        using (var transaction = session.BeginTransaction())
                        {
                            ticketLog.TicketLogStopDate = DateTime.Now.ToUniversalTime();
                            session.Update(ticket);
                            session.Update(ticketLog);
                            transaction.Commit();
                        }
                    }
                }
            }, 5);
        }

        private static string generateCoreFileName(RingCentral.CallLog.CallLogData.Record rec)
        {
            var fileName = "";

            var from = "";
            var to = "";
            if (rec.from != null && !string.IsNullOrEmpty(rec.from.phoneNumber))
                from = rec.from.phoneNumber.Replace(" ", "").Replace("(", "").Replace(")", "").Replace("-", "").Replace("+", "");
            if (rec.to != null && !string.IsNullOrEmpty(rec.to.phoneNumber))
                to = rec.to.phoneNumber.Replace(" ", "").Replace("(", "").Replace(")", "").Replace("-", "").Replace("+", "");
            if (to.Length == 11)
                to = to.Substring(1, 10);
            if (from.Length == 11)
                from = from.Substring(1, 10);
            if (to.Length == 10)
                to = "(" + to.Substring(0, 3) + ") " + to.Substring(3, 3) + "-" + to.Substring(6, 4);
            if (from.Length == 10)
                from = "(" + from.Substring(0, 3) + ") " + from.Substring(3, 3) + "-" + from.Substring(6, 4);
            if (from.Length == 0 && rec.from != null && !string.IsNullOrEmpty(rec.from.extensionNumber))
                from = "x" + rec.from.extensionNumber;
            if (to.Length == 0 && rec.to != null && !string.IsNullOrEmpty(rec.to.extensionNumber))
                to = "x" + rec.to.extensionNumber;
            if (string.IsNullOrWhiteSpace(from))
                from = "Unknown";
            if (string.IsNullOrWhiteSpace(to))
                to = "Unknown";
            from = from.Replace(" ", "");
            to = to.Replace(" ", "");

            DateTime time;
            fileName += (DateTime.TryParse(rec.startTime, out time) ? time.ToUniversalTime() : DateTime.Now.ToUniversalTime()).ToString("yyyyMMdd_HHmm");
            fileName += ("_" + from + "_" + to + "_" + rec.direction + "_" + rec.result).Replace(" ", "-");

            return fileName;
        }

        private static string generateCoreFileName(RingCentral.MessageStore.MessageStoreData.Record rec)
        {
            var fileName = "";

            var from = "";
            var to = "";
            var totalRecepients = 0;
            if (rec.from != null && !string.IsNullOrEmpty(rec.from.phoneNumber))
                from = rec.from.phoneNumber.Replace(" ", "").Replace("(", "").Replace(")", "").Replace("-", "").Replace("+", "");
            if (rec.to != null && rec.to.Any())
            {
                totalRecepients = rec.to.Count();
                if (totalRecepients == 1)
                {
                    var firstPhone = rec.to.First(x => !string.IsNullOrEmpty(x.phoneNumber));
                    to = firstPhone.phoneNumber.Replace(" ", "").Replace("(", "").Replace(")", "").Replace("-", "").Replace("+", "");
                }
                else if (totalRecepients > 1)
                {
                    to = "MultipleRecipients";
                }
            }
            if (to.Length == 11)
                to = to.Substring(1, 10);
            if (from.Length == 11)
                from = from.Substring(1, 10);
            if (to.Length == 10)
                to = "(" + to.Substring(0, 3) + ") " + to.Substring(3, 3) + "-" + to.Substring(6, 4);
            if (from.Length == 10)
                from = "(" + from.Substring(0, 3) + ") " + from.Substring(3, 3) + "-" + from.Substring(6, 4);
            if (string.IsNullOrWhiteSpace(from))
                from = "Unknown";
            if (string.IsNullOrWhiteSpace(to))
                to = "Unknown";
            from = from.Replace(" ", "");
            to = to.Replace(" ", "");

            fileName += DateTime.Parse(rec.creationTime).ToUniversalTime().ToString("yyyyMMdd_HHmm");
            fileName += ("_" + from + "_" + to + "_" + rec.direction + "_" + rec.messageStatus).Replace(" ", "-");

            return fileName;
        }

        private static string getFileExtension(string contentType, string defaultExtension = "txt")
        {
            var extension = defaultExtension;

            if (contentType == "audio/mpeg")
                extension = "mp3";
            else if (contentType == "text/plain" || contentType == "text/html")
                extension = "txt";
            else if (contentType == "application/pdf")
                extension = "pdf";
            else if (contentType == "image/jpeg")
                extension = "jpg";
            else if (contentType == "image/gif")
                extension = "gif";
            else if (contentType == "video/3gpp" || contentType == "video/3gp")
                extension = "3gp";

            return extension;
        }

        private static void LogWriteLine(string text, LogTextStatus status, TextWriter log, TicketLog ticketLog, bool enableDatabaseLogging)
        {
            if (enableDatabaseLogging)
            {
                var cls = "I";
                if (status != LogTextStatus.None)
                {
                    if (status == LogTextStatus.Danger)
                        cls = "D";
                    else if (status == LogTextStatus.Info)
                        cls = "I";
                    else if (status == LogTextStatus.Success)
                        cls = "S";
                    else if (status == LogTextStatus.Warning)
                        cls = "W";
                }
                if (ticketLog.LogText == null)
                    ticketLog.LogText = "";
                if (ticketLog.LogText.Length < 5000)
                {
                    var time = 1;
                    TryNTimes(delegate {
                        using (ISessionFactory sessionFactory = CreateSessionFactory())
                        {
                            using (var session = sessionFactory.OpenStatelessSession())
                            {
                                var textPlusDate = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd hh:mm:ss:ff tt") + " " + cls + " " + text;
                                log.WriteLine(time++.ToString("0") + "_" + ticketLog.TicketLogId.ToString("000000") + "_" + ticketLog.TicketLogId.ToString("000000") + " - " + textPlusDate);
                                ticketLog.LogText += (!string.IsNullOrEmpty(ticketLog.LogText) ? Environment.NewLine : "") + textPlusDate;
                                if (ticketLog.LogText.Length > 5000)
                                    ticketLog.LogText = ticketLog.LogText.Substring(0, 5000);
                                if (ticketLog.TicketLogId > 0)
                                {
                                    using (var transaction = session.BeginTransaction())
                                    {
                                        session.Update(ticketLog);
                                        transaction.Commit();
                                    }
                                }
                            }
                        }
                    }, 5);
                }
            }
        }

        private static void addTraceForLog(int ticketLogId, int ticketId, int transferBatchId, string traceText)
        {
            using (ISessionFactory sessionFactory = CreateSessionFactory())
            {
                using (var session = sessionFactory.OpenStatelessSession())
                {
                    using (var transaction = session.BeginTransaction())
                    {
                        var traceRec = new TicketLogTrace();
                        traceRec.TicketId = ticketId;
                        traceRec.TicketLogId = ticketLogId;
                        traceRec.TransferBatchId = transferBatchId;
                        traceRec.TraceText = traceText;
                        session.Insert(traceRec);
                        transaction.Commit();
                    }
                }
            }
        }

        private static int getAccountIdFromTicket(Ticket ticket)
        {
            int id = 0;
            if (ticket.Destination == "google")
            {
                using (ISessionFactory sessionFactory = CreateSessionFactory())
                {
                    using (var session = sessionFactory.OpenSession())
                    {
                        var googleAccountCrit = session.CreateCriteria<GoogleAccountRec>();
                        googleAccountCrit.Add(Expression.Eq("GoogleAccountId", ticket.DestinationGoogleAccountId));
                        var googleAccounts = googleAccountCrit.List<GoogleAccountRec>();
                        if (googleAccounts.Any())
                        {
                            var googleAccount = googleAccounts.First();
                            id = googleAccount.AccountId;
                        }
                    }
                }

            }
            else if (ticket.Destination == "amazon")
            {
                using (ISessionFactory sessionFactory = CreateSessionFactory())
                {
                    using (var session = sessionFactory.OpenSession())
                    {
                        var amazonAccountCrit = session.CreateCriteria<AmazonAccountRec>();
                        amazonAccountCrit.Add(Expression.Eq("AmazonAccountId", ticket.DestinationAmazonAccountId));
                        var amazonAccounts = amazonAccountCrit.List<AmazonAccountRec>();
                        if (amazonAccounts.Any())
                        {
                            var amazonAccount = amazonAccounts.First();
                            id = amazonAccount.AccountId;
                        }
                    }
                }
            }
            else if (ticket.Destination == "box")
            {
                using (ISessionFactory sessionFactory = CreateSessionFactory())
                {
                    using (var session = sessionFactory.OpenSession())
                    {
                        var boxAccountCrit = session.CreateCriteria<BoxAccountRec>();
                        boxAccountCrit.Add(Expression.Eq("BoxAccountId", ticket.DestinationBoxAccountId));
                        var boxAccounts = boxAccountCrit.List<BoxAccountRec>();
                        if (boxAccounts.Any())
                        {
                            var boxAccount = boxAccounts.First();
                            id = boxAccount.AccountId;
                        }
                    }
                }
            }
            return id;
        }

        private static void handleNameCollisions(Ticket ticket)
        {
            using (ISessionFactory sessionFactory = CreateSessionFactory())
            {
                using (var session = sessionFactory.OpenSession())
                {
                    var ticketCriteria = session.CreateCriteria<Ticket>();
                    ticketCriteria.Add(Expression.Not(Expression.Eq("TicketId", ticket.TicketId)));
                    ticketCriteria.Add(Expression.Eq("SaveAsFileName", ticket.SaveAsFileName));
                    ticketCriteria.Add(Expression.Eq("DeletedInd", false));
                    var ticketsWithSameName = ticketCriteria.List<Ticket>();
                    if (ticketsWithSameName.Any())
                    {
                        var processedTickets = ticketsWithSameName.Where(t => t.CompleteDate.HasValue && t.ErrorInd == false);
                        if (!string.IsNullOrEmpty(ticket.CallId))
                        {
                            if (processedTickets.Any(x => x.CallId == ticket.CallId))
                            {
                                var duplicateTicket = processedTickets.First(x => x.CallId == ticket.CallId);
                                ticket.NameIteration = duplicateTicket.NameIteration;
                            }
                            else if (processedTickets.Any())
                            {
                                ticket.NameIteration = processedTickets.Max(x => x.NameIteration) + 1;
                            }
                        }
                        else if (!string.IsNullOrEmpty(ticket.MessageId))
                        {
                            if (processedTickets.Any(x => x.MessageId == ticket.MessageId))
                            {
                                var duplicateTicket = processedTickets.First(x => x.MessageId == ticket.MessageId);
                                ticket.NameIteration = duplicateTicket.NameIteration;
                            }
                            else if (processedTickets.Any())
                            {
                                ticket.NameIteration = processedTickets.Max(x => x.NameIteration) + 1;
                            }
                        }
                    }
                }
            }
        }

        private static void AddToIndex(Archive index)
        {
            if (index == null)
                return;
            if (index.Files != null)
            {
                foreach (var file in index.Files)
                {
                    if (file.Destination == "google")
                        file.ContentUri = string.Format("{0}/files/{1}", RingClone.AppConfig.Google.ApiUri, file.FileId);
                    else if (file.Destination == "box")
                        file.ContentUri = string.Format("{0}/files/{1}/content", RingClone.AppConfig.Box.ApiUri, file.FileId);
                    else if (file.Destination == "amazon")
                        file.ContentUri = string.Format("{0}.s3.amazonaws.com/{1}", file.BucketName, file.Filename);
                }
            }
            var rawData = JsonConvert.SerializeObject(index);
            IndexRecord indexRec = null;
            IndexRawDataRecord rawDataRec = null;
            using (ISessionFactory sessionFactory = CreateSessionFactory())
            {
                using (var session = sessionFactory.OpenSession())
                {
                    var archiveRecCriteria = session.CreateCriteria<IndexRecord>();
                    archiveRecCriteria.Add(Expression.Eq("CallId", index.CallId));
                    archiveRecCriteria.Add(Expression.Eq("MessageId", index.MessageId));
                    archiveRecCriteria.Add(Expression.Eq("OwnerId", index.Owner));
                    indexRec = archiveRecCriteria.UniqueResult<IndexRecord>();
                    if (indexRec == null)
                        indexRec = new IndexRecord()
                        {
                            CallId = index.CallId,
                            MessageId = index.MessageId,
                            OwnerId = index.Owner                             
                        };
                    indexRec.CallTime = index.CallTime;
                    indexRec.DefaultFileName = index.DefaultFileName;
                    indexRec.DeletedInd = false;
                    indexRec.TicketId = index.TicketId;
                    indexRec.TicketLogId = index.TicketLogId;
                    indexRec.Type = index.Type;
                    if (indexRec.IndexRawDataId != 0)
                    {
                        var rawDataRecCriteria = session.CreateCriteria<IndexRawDataRecord>();
                        rawDataRecCriteria.Add(Expression.Eq("IndexRawDataId", indexRec.IndexRawDataId));
                        rawDataRec = rawDataRecCriteria.UniqueResult<IndexRawDataRecord>();
                    }
                    if (rawDataRec == null)
                    {
                        rawDataRec = new IndexRawDataRecord();
                    }
                    rawDataRec.RawData = JsonConvert.SerializeObject(index);
                    using (var transaction = session.BeginTransaction())
                    {
                        session.SaveOrUpdate(rawDataRec);
                        indexRec.IndexRawDataId = rawDataRec.IndexRawDataId;
                        session.SaveOrUpdate(indexRec);
                        transaction.Commit();
                    }
                }
            }
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConnectionStringHelper.StorageConnectionString);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue queue = queueClient.GetQueueReference("ringcentralindexerqueue");
            queue.CreateIfNotExists();
            CloudQueueMessage message = new CloudQueueMessage("{TicketId:" + index.TicketId + "}");
            queue.AddMessage(message);
        }

        static void TryNTimes(Action func, int times)
        {
            while (times > 0)
            {
                try
                {
                    func();
                    return;
                }
                catch (Exception e)
                {
                    if (--times <= 0)
                        throw e;
                }
            }
        }

        private static void saveToCache(string key, object obj)
        {
            if (MemoryCache.Default.Get(key) == null)
            {
                var policy = new CacheItemPolicy
                {
                    AbsoluteExpiration = DateTimeOffset.UtcNow.AddMilliseconds(600000)//10 min
                };
                MemoryCache.Default.Set(key, obj, policy);
            }
        }
        private static object getFromCache(string key)
        {
            return MemoryCache.Default.Get(key);
        }
        public enum LogTextStatus
		{
			None,
			Danger,
			Warning,
			Info,
			Success
		}

        public class ThingToSave
        {
            public string Id;
            public string ContentType;
            public string ContentUri;
            public string RawData;
            public string Filename;
            public DateTime Timestamp;
        }

        public class TicketSourceAndDest
        {
            public AccountRec account;
            public AccountRec accountForDestination;
            public BoxAccountRec boxAccount;
            public GoogleAccountRec googleAccount;
            public AmazonAccountRec amazonAccount;
        }

        #region NHibernate
        public static ISessionFactory CreateSessionFactory()
		{
			string connString = ConnectionStringHelper.ConnectionString;
			return Fluently.Configure()
				.Database(MsSqlConfiguration.MsSql2008
					.ConnectionString(connString)
				)
				.Cache(c => c
					.UseQueryCache())
				.Mappings(m => m.FluentMappings.AddFromAssemblyOf<Functions>())
                .Diagnostics(x => x.Disable())
				.BuildSessionFactory();
		}
		#endregion

		#region Database Models


		public class AccountRec
		{
			public virtual int AccountId { get; set; }
			public virtual string RingCentralId { get; set; }
			public virtual string RingCentralExtension { get; set; }
            public virtual int RingCentralTokenId { get; set; }
            public virtual string RingCentralOwnerId { get; set; }
        }
        private class AccountRecMap : ClassMap<AccountRec>
		{
			public AccountRecMap()
			{
				Table("T_ACCOUNT");
				Id(x => x.AccountId).Column("AccountId");
				Map(x => x.RingCentralId);
                Map(x => x.RingCentralTokenId);
                Map(x => x.RingCentralOwnerId);
            }
        }

		public class BoxAccountRec
		{
			public virtual int BoxAccountId { get; set; }
			public virtual int BoxTokenId { get; set; }
			public virtual int AccountId { get; set; }
			public virtual string BoxAccountName { get; set; }
			public virtual bool DeletedInd { get; set; }
			public virtual bool ActiveInd { get; set; }
		}
		private class BoxAccountRecMap : ClassMap<BoxAccountRec>
		{
			public BoxAccountRecMap()
			{
				Table("T_BOXACCOUNT");
				Id(x => x.BoxAccountId);
				Map(x => x.BoxTokenId);
				Map(x => x.BoxAccountName);
				Map(x => x.AccountId);
				Map(x => x.DeletedInd);
				Map(x => x.ActiveInd);
			}
		}

		public class GoogleAccountRec
		{
			public virtual int GoogleAccountId { get; set; }
			public virtual int GoogleTokenId { get; set; }
			public virtual int AccountId { get; set; }
			public virtual string GoogleAccountName { get; set; }
			public virtual bool DeletedInd { get; set; }
			public virtual bool ActiveInd { get; set; }
		}
		private class GoogleAccountRecMap : ClassMap<GoogleAccountRec>
		{
			public GoogleAccountRecMap()
			{
				Table("T_GOOGLEACCOUNT");
				Id(x => x.GoogleAccountId);
				Map(x => x.GoogleTokenId);
				Map(x => x.GoogleAccountName);
				Map(x => x.AccountId);
				Map(x => x.DeletedInd);
				Map(x => x.ActiveInd);
			}
		}

        public class AmazonAccountRec
        {
            public virtual int AmazonAccountId { get; set; }
            public virtual int AmazonUserId { get; set; }
            public virtual int AccountId { get; set; }
            public virtual string AmazonAccountName { get; set; }
            public virtual bool DeletedInd { get; set; }
            public virtual bool ActiveInd { get; set; }
        }
        private class AmazonAccountRecMap : ClassMap<AmazonAccountRec>
        {
            public AmazonAccountRecMap()
            {
                Table("T_AMAZONACCOUNT");
                Id(x => x.AmazonAccountId);
                Map(x => x.AmazonUserId);
                Map(x => x.AmazonAccountName);
                Map(x => x.AccountId);
                Map(x => x.DeletedInd);
                Map(x => x.ActiveInd);
            }
        }
        public class AmazonUserRec
        {
            public virtual int AmazonUserId { get; set; }
            public virtual string Region { get; set; }
            public virtual string AccessKeyId { get; set; }
            public virtual string SecretAccessKey { get; set; }
            public virtual bool DeletedInd { get; set; }
        }
        private class AmazonUserRecMap : ClassMap<AmazonUserRec>
        {
            public AmazonUserRecMap()
            {
                Table("T_AMAZONUSER");
                Id(x => x.AmazonUserId);
                Map(x => x.Region);
                Map(x => x.AccessKeyId);
                Map(x => x.SecretAccessKey);
                Map(x => x.DeletedInd);
            }
        }

        public class TransferBatch
		{
			public virtual int TransferBatchId { get; set; }
			public virtual int TransferRuleId { get; set; }
			public virtual int AccountId { get; set; }
			public virtual bool QueuedInd { get; set; }
			public virtual bool ProcessingInd { get; set; }
            public virtual bool DeletedInd { get; set; }
            public virtual bool RedoInd { get; set; }
            public virtual DateTime CreateDate { get; set; }
            public virtual bool ErrorInd { get; set; }
            public virtual DateTime? CompleteDate { get; set; }
            public virtual IList<Ticket> Tickets { get; set; }
        }
        private class TransferBatchMap : ClassMap<TransferBatch>
		{
			public TransferBatchMap()
			{
				Table("T_TRANSFERBATCH");
				Id(x => x.TransferBatchId).Column("TransferBatchId");
				Map(x => x.TransferRuleId);
				Map(x => x.AccountId);
				Map(x => x.QueuedInd);
				Map(x => x.ProcessingInd);
                Map(x => x.DeletedInd);
                Map(x => x.RedoInd);
                Map(x => x.CreateDate);
                Map(x => x.ErrorInd);
                Map(x => x.CompleteDate);
            }
        }

		public class Ticket
		{
			public virtual int TicketId { get; set; }
			public virtual int TransferBatchId { get; set; }
			public virtual DateTime CreateDate { get; set; }
			public virtual string InitiatedBy { get; set; }
			public virtual string Destination { get; set; }
			public virtual int DestinationBoxAccountId { get; set; }
			public virtual int DestinationGoogleAccountId { get; set; }
			public virtual int DestinationFtpAccountId { get; set; }
			public virtual string DestinationFolderId { get; set; }
            public virtual string DestinationFolderLabel { get; set; }
            public virtual int DestinationAmazonAccountId { get; set; }
            public virtual string DestinationBucketName { get; set; }
            public virtual string DestinationPrefix { get; set; }
            public virtual bool PutInDatedSubfolder { get; set; }
			public virtual string SaveAsFileName { get; set; }
            public virtual string CallId { get; set; }
            public virtual string MessageId { get; set; }
            public virtual DateTime? CallTime { get; set; }
            public virtual string Type { get; set; }
            public virtual bool LogInd { get; set; }
            public virtual bool ContentInd { get; set; }
            public virtual bool ProcessingInd { get; set; }
            public virtual bool DeletedInd { get; set; }
            public virtual bool RedoInd { get; set; }
            public virtual int NameIteration { get; set; }
            public virtual bool ErrorInd { get; set; }
            public virtual DateTime? CompleteDate { get; set; }
        }
        private class TicketMap : ClassMap<Ticket>
		{
			public TicketMap()
			{
				Table("T_TICKET");
				Id(x => x.TicketId).Column("TicketId");
				Map(x => x.TransferBatchId);
				Map(x => x.CreateDate);
				Map(x => x.InitiatedBy);
				Map(x => x.Destination);
				Map(x => x.DestinationBoxAccountId);
				Map(x => x.DestinationGoogleAccountId);
				Map(x => x.DestinationFtpAccountId);
				Map(x => x.DestinationFolderId);
                Map(x => x.DestinationFolderLabel);
                Map(x => x.DestinationAmazonAccountId);
                Map(x => x.DestinationBucketName);
                Map(x => x.DestinationPrefix);
                Map(x => x.PutInDatedSubfolder);
				Map(x => x.SaveAsFileName);
                Map(x => x.CallId);
                Map(x => x.CallTime);
                Map(x => x.MessageId);
                Map(x => x.Type);
                Map(x => x.LogInd);
                Map(x => x.ContentInd);
                Map(x => x.ProcessingInd);
                Map(x => x.DeletedInd);
                Map(x => x.RedoInd);
                Map(x => x.NameIteration);
                Map(x => x.ErrorInd);
                Map(x => x.CompleteDate);
            }
        }

        private class TicketRawDataRec
        {
            public virtual int TicketRawDataId { get; set; }
            public virtual int TicketId { get; set; }
            public virtual int TransferBatchId { get; set; }
            public virtual string RawData { get; set; }
            public virtual bool DeletedInd { get; set; }
        }
        private class TicketRawDataRecMap : ClassMap<TicketRawDataRec>
        {
            public TicketRawDataRecMap()
            {
                Table("T_TICKETRAWDATA");
                Id(x => x.TicketRawDataId).Column("TicketRawDataId");
                Map(x => x.TicketId);
                Map(x => x.TransferBatchId);
                Map(x => x.RawData).Length(Int32.MaxValue);
                Map(x => x.DeletedInd);
            }
        }

		public class TicketLog
		{
			public virtual int TicketLogId { get; set; }
			public virtual int TicketId { get; set; }
			public virtual int TransferBatchId { get; set; }
			public virtual DateTime? TicketLogStartDate { get; set; }
			public virtual DateTime? TicketLogStopDate { get; set; }
			public virtual bool ErrorInd { get; set; }
			public virtual string Message { get; set; }
			public virtual string LogText { get; set; }
		}
		private class TicketLogMap : ClassMap<TicketLog>
		{
			public TicketLogMap()
			{
				Table("T_TICKETLOG");
				Id(x => x.TicketLogId).Column("TicketLogId");
				Map(x => x.TicketId);
				Map(x => x.TransferBatchId);
				Map(x => x.TicketLogStartDate);
				Map(x => x.TicketLogStopDate);
				Map(x => x.ErrorInd);
				Map(x => x.Message).Length(500);
				Map(x => x.LogText).Length(5000);
			}
		}

        public class TicketLogTrace
        {
            public virtual int TicketLogTraceId { get; set; }
            public virtual int TicketLogId { get; set; }
            public virtual int TicketId { get; set; }
            public virtual int TransferBatchId { get; set; }
            public virtual string TraceText { get; set; }
            public virtual bool DeletedInd { get; set; }
        }
        private class TicketLogTraceMap : ClassMap<TicketLogTrace>
        {
            public TicketLogTraceMap()
            {
                Table("T_TICKETLOGTRACE");
                Id(x => x.TicketLogTraceId).Column("TicketLogTraceId");
                Map(x => x.TicketLogId);
                Map(x => x.TicketId);
                Map(x => x.TransferBatchId);
                Map(x => x.TraceText);
                Map(x => x.DeletedInd);
            }
        }

        public class IndexRecord
        {
            public virtual int IndexId { get; set; }
            public virtual string OwnerId { get; set; }
            public virtual int TicketId { get; set; }
            public virtual int TicketLogId { get; set; }
            public virtual string Type { get; set; }
            public virtual string CallId { get; set; }
            public virtual string MessageId { get; set; }
            public virtual DateTime CallTime { get; set; }
            public virtual int IndexRawDataId { get; set; }
            public virtual int IndexMessageId { get; set; }
            public virtual string DefaultFileName { get; set; }
            public virtual bool DeletedInd { get; set; }
        }
        private class IndexRecordMap : ClassMap<IndexRecord>
        {
            public IndexRecordMap()
            {
                Table("T_INDEX");
                Id(x => x.IndexId);
                Map(x => x.OwnerId);
                Map(x => x.TicketId);
                Map(x => x.TicketLogId);
                Map(x => x.Type);
                Map(x => x.CallId);
                Map(x => x.MessageId);
                Map(x => x.CallTime);
                Map(x => x.IndexRawDataId);
                Map(x => x.IndexMessageId);
                Map(x => x.DefaultFileName);
                Map(x => x.DeletedInd);
            }
        }
        public class IndexRawDataRecord
        {
            public virtual int IndexRawDataId { get; set; }
            public virtual string RawData { get; set; }
        }
        private class IndexRawDataRecordMap : ClassMap<IndexRawDataRecord>
        {
            public IndexRawDataRecordMap()
            {
                Table("T_INDEXRAWDATA");
                Id(x => x.IndexRawDataId);
                Map(x => x.RawData).Length(8000);
            }
        }

        #endregion

        #region Other Models
        public class Archive
        {
            public string Owner;
            public int TicketId;
            public int TicketLogId;
            public string Type;
            public string CallId;
            public string MessageId;
            public DateTime CallTime;
            public string DefaultFileName;
            public int TimeElapsedForTransfer;
            public int TimeElapsedForIndex;
            public RingCentral.CallLog.CallLogData.Record CallLog;
            public RingCentral.MessageStore.MessageStoreData.Record MessageLog;
            public List<ArchiveFile> Files;
            public class ArchiveFile
            {
                public bool LogInd;
                public bool ContentInd;
                public int NumberOfBytes;
                public string Destination;
                public int DestinationAccountId;
                public string ContentUri;
                public string Filename;
                public string FileId;
                public string Folder;
                public string BucketName;
                public string Prefix;
            }
        }
        #endregion
    }
}
