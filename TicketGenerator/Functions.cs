using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using NHibernate;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using FluentNHibernate.Mapping;
using NHibernate.Criterion;
using System.Web.Script.Serialization;

namespace TicketGenerator
{
	public class Functions
	{
		// This function will get triggered/executed when a new message is written 
		// on an Azure Queue called queue.
		public static void ProcessQueueMessage([QueueTrigger("queue")] string message, TextWriter log)
		{
			log.WriteLine(message);
		}

		public static void getTickets(TicketGeneratorModel model)
		{
			if (model != null && model.BatchesToRun != null && model.BatchesToRun.Any(x => !x.AccountIsInactive))
			{
				foreach (var batch in model.BatchesToRun.Where(x => !x.AccountIsInactive))
				{
					batch.Tickets = new List<TicketGeneratorModel.Ticket>();
                    getExtensions(batch);
                    if (batch.Rule.VoiceLogInd || batch.Rule.VoiceContentInd)
                        getVoiceLogs(batch);
                    if (batch.Rule.FaxLogInd || batch.Rule.FaxContentInd)
                        getFaxLogs(batch);
                    if (batch.Rule.SmsLogInd || batch.Rule.SmsContentInd)
                        getSmsLogs(batch);
                    foreach (var logEntry in batch.LogEntries)
					{
						if (!logEntry.Archived)
						{
                            if ((logEntry.Type == "voice" && (batch.Rule.VoiceLogInd || (batch.Rule.VoiceContentInd && logEntry.HasContent))) || 
                                (logEntry.Type == "fax" && (batch.Rule.FaxLogInd || (batch.Rule.FaxContentInd && logEntry.HasContent))) ||
                                (logEntry.Type == "sms" && (batch.Rule.SmsLogInd || (batch.Rule.SmsContentInd && logEntry.HasContent))))
                            {
                                var ticket = new TicketGeneratorModel.Ticket()
                                {
                                    CreateDate = DateTime.Now.ToUniversalTime(),
                                    Destination = batch.Rule.Destination,
                                    DestinationBoxAccountId = batch.Rule.DestinationBoxAccountId,
                                    DestinationGoogleAccountId = batch.Rule.DestinationGoogleAccountId,
                                    DestinationAmazonAccountId = batch.Rule.DestinationAmazonAccountId,
                                    DestinationFolderId = batch.Rule.DestinationFolderId,
                                    DestinationFolderLabel = batch.Rule.DestinationFolderLabel,
                                    DestinationBucketName = batch.Rule.DestinationBucketName,
                                    DestinationPrefix = batch.Rule.DestinationPrefix,
                                    PutInDatedSubfolder = batch.Rule.PutInDatedSubfolder,
                                    CallId = logEntry.Type == "voice" ? logEntry.Id : null,
                                    MessageId = logEntry.Type != "voice" ? logEntry.Id : null,
                                    InitiatedBy = "system",
                                    Type = logEntry.Type,
                                    LogInd = (batch.Rule.VoiceLogInd && logEntry.Type == "voice") || (batch.Rule.FaxLogInd && logEntry.Type == "fax") || (batch.Rule.SmsLogInd && logEntry.Type == "sms"),
                                    ContentInd = (batch.Rule.VoiceContentInd && logEntry.Type == "voice") || (batch.Rule.FaxContentInd && logEntry.Type == "fax") || (batch.Rule.SmsContentInd && logEntry.Type == "sms"),
                                    SaveAsFilename = logEntry.SaveAsFilename,
                                    RawData = logEntry.RawData
                                };
                                batch.Tickets.Add(ticket);
                            }
                        }
					}
				}
			}
		}

		public static void getBatchesToAdd(TicketGeneratorModel model)
		{

			if (model.BatchesToRun == null)
				model.BatchesToRun = new List<TicketGeneratorModel.TransferBatch>();

			using (ISessionFactory sessionFactory = CreateSessionFactory())
			{
				using (var session = sessionFactory.OpenSession())
				{
					//GET ALL RULES THAT NEED TO BE RUN THIS HOUR
					var ruleCrit = session.CreateCriteria<TransferRuleRec>();
					var ruleDisjunction = new Disjunction();
					//ACTIVE AND NOT DELETED
					ruleCrit.Add(Expression.Eq("DeletedInd", false));
					ruleCrit.Add(Expression.Eq("ActiveInd", true));

					var rules = ruleCrit.List<TransferRuleRec>();

					if (rules.Any())
					{
						foreach (var ruleRec in rules)
						{
							bool isDayToRun = false;
							bool isTimeToRun = false;
							var clientDaysInThisMonth = DateTime.DaysInMonth(model.Now.Year, model.Now.Month);
							//MONTHLY RULES
							if (ruleRec.Frequency == "First day of each month" && model.Now.Day == 1)
								isDayToRun = true;
							if (ruleRec.Frequency == "Last day of each month" && model.Now.Day == clientDaysInThisMonth)
								isDayToRun = true;
							if (ruleRec.Frequency == "Middle of each month (15th)" && model.Now.Day == 15)
								isDayToRun = true;
							//WEEKLY RULES
							var clientDayOfWeek = model.Now.ToString("dddd");
							if (ruleRec.Frequency == "Every " + clientDayOfWeek)
								isDayToRun = true;
							//DAILY RULE
							if (ruleRec.Frequency == "Every day")
								isDayToRun = true;
							//TIME OF DAY
							var clientTimeOfDayForRightNow = model.Now.ToString("HH00"); // "0000", "0300", "1600", etc;
							if (ruleRec.TimeOfDay == clientTimeOfDayForRightNow)
								isTimeToRun = true;

							if (isDayToRun && isTimeToRun)
							{
								//HAS THIS RULE BEEN RUN THIS HOUR?
								var batchCrit = session.CreateCriteria<TransferBatchRec>();
								batchCrit.Add(Expression.Eq("TransferRuleId", ruleRec.TransferRuleId));
								var startOfHour = new DateTime(model.Now.Year, model.Now.Month, model.Now.Day, model.Now.Hour, 0, 0, 0);
								var endOfHour = new DateTime(model.Now.Year, model.Now.Month, model.Now.Day, model.Now.Hour, 59, 59, 999);
								batchCrit.Add(Expression.Ge("CreateDate", startOfHour));
								batchCrit.Add(Expression.Le("CreateDate", endOfHour));
								var batches = batchCrit.List<TransferBatchRec>();
								//IF NOT, ADD IT TO THE LIST
								if (!batches.Any())
								{
									var rule = new TicketGeneratorModel.Rule();
									rule.AccountId = ruleRec.AccountId;
									rule.ActiveInd = ruleRec.ActiveInd;
									rule.DayOf = ruleRec.DayOf;
									rule.DeletedInd = ruleRec.DeletedInd;
									rule.Destination = ruleRec.Destination;
									rule.DestinationBoxAccountId = ruleRec.DestinationBoxAccountId;
									rule.DestinationFolderId = ruleRec.DestinationFolderId;
									rule.DestinationFolderLabel = ruleRec.DestinationFolderLabel;
									rule.DestinationFtpAccountId = ruleRec.DestinationFtpAccountId;
                                    rule.DestinationGoogleAccountId = ruleRec.DestinationGoogleAccountId;
                                    rule.DestinationAmazonAccountId = ruleRec.DestinationAmazonAccountId;
                                    rule.DestinationBucketName = ruleRec.DestinationBucketName;
                                    rule.DestinationPrefix = ruleRec.DestinationPrefix;
                                    rule.PutInDatedSubfolder = ruleRec.PutInDatedSubFolder;
									rule.Frequency = ruleRec.Frequency;
									rule.Source = ruleRec.Source;
									rule.TimeOfDay = ruleRec.TimeOfDay;
									rule.TransferRuleId = ruleRec.TransferRuleId;
                                    rule.VoiceLogInd = ruleRec.VoiceLogInd;
                                    rule.VoiceContentInd = ruleRec.VoiceContentInd;
                                    rule.FaxLogInd = ruleRec.FaxLogInd;
                                    rule.FaxContentInd = ruleRec.FaxContentInd;
                                    rule.SmsLogInd = ruleRec.SmsLogInd;
                                    rule.SmsContentInd = ruleRec.SmsContentInd;
                                    var newBatch = new TicketGeneratorModel.TransferBatch();
									newBatch.AccountId = ruleRec.AccountId;
									newBatch.Destination = ruleRec.Destination;
									newBatch.Frequency = ruleRec.Frequency;
									newBatch.Source = ruleRec.Source;
									newBatch.TimeOfDay = ruleRec.TimeOfDay;
									newBatch.Rule = rule;
									model.BatchesToRun.Add(newBatch);
								}
							}
						}
					}
				}
			}
		}

		public static void getAccountInfoForBatches(TicketGeneratorModel model)
		{

			if (model.BatchesToRun != null && model.BatchesToRun.Any())
			{
				ICollection<Account> accounts = new List<Account>();
				using (ISessionFactory sessionFactory = CreateSessionFactory())
				{
					using (var session = sessionFactory.OpenSession())
					{
						var accountCrit = session.CreateCriteria<Account>();
						accountCrit.Add(Expression.In("AccountId", model.BatchesToRun.Select(x => x.AccountId).ToArray()));
						accounts = accountCrit.List<Account>();
					}
				}
				foreach (var batch in model.BatchesToRun)
				{
					var account = accounts.First(x => x.AccountId == batch.AccountId);
					batch.RingCentralId = account.RingCentralId;
					if (!account.ActiveInd || account.CancelledInd || account.DeletedInd || !account.PaymentIsCurrentInd)
						batch.AccountIsInactive = true;
				}
			}
		}

		public static void saveBatchesAndTickets(TicketGeneratorModel model)
		{
			if (model.BatchesToRun.Any())
			{
				foreach (var batch in model.BatchesToRun)
				{
					using (ISessionFactory sessionFactory = CreateSessionFactory())
					{
						using (var session = sessionFactory.OpenSession())
						{
							using (var transaction = session.BeginTransaction())
							{
                                //GET NEXT LOG NUMBER
                                var maxLogNumber = session.CreateCriteria<TransferBatchRec>()
                                    .Add(Expression.Eq("AccountId", batch.AccountId))
                                    .SetProjection(Projections.Max("LogNumber"))
                                    .UniqueResult<int>();
                                var nextLogNumber = maxLogNumber + 1;
                                //CREATE BATCH
                                var newBatchRec = new TransferBatchRec();
								newBatchRec.AccountId = batch.AccountId;
								newBatchRec.CreateDate = System.DateTime.Now.ToUniversalTime();
								newBatchRec.TransferRuleId = batch.Rule.TransferRuleId;
								newBatchRec.QueuedInd = true;
                                newBatchRec.LogNumber = nextLogNumber;
								session.Save(newBatchRec);
                                //CREATE TICKETS
								foreach (var ticket in batch.Tickets)
								{
									var newTicketRec = new TicketRec();
									newTicketRec.CreateDate = DateTime.Now.ToUniversalTime();
									newTicketRec.Destination = ticket.Destination;
									newTicketRec.DestinationBoxAccountId = ticket.DestinationBoxAccountId;
                                    newTicketRec.DestinationGoogleAccountId = ticket.DestinationGoogleAccountId;
                                    newTicketRec.DestinationAmazonAccountId = ticket.DestinationAmazonAccountId;
                                    newTicketRec.DestinationFolderId = ticket.DestinationFolderId;
                                    newTicketRec.DestinationFolderLabel = ticket.DestinationFolderLabel;
                                    newTicketRec.DestinationBucketName = ticket.DestinationBucketName;
                                    newTicketRec.DestinationPrefix = ticket.DestinationPrefix;
                                    newTicketRec.PutInDatedSubfolder = ticket.PutInDatedSubfolder;
									newTicketRec.CallId = ticket.CallId;
									newTicketRec.InitiatedBy = ticket.InitiatedBy;
									newTicketRec.TransferBatchId = newBatchRec.TransferBatchId;
                                    newTicketRec.ContentInd = ticket.ContentInd;
                                    newTicketRec.LogInd = ticket.LogInd;
                                    newTicketRec.MessageId = ticket.MessageId;
                                    newTicketRec.Type = ticket.Type;
                                    newTicketRec.SaveAsFilename = ticket.SaveAsFilename;
									session.SaveOrUpdate(newTicketRec);
                                    var ticketRawData = new TicketRawDataRec()
                                    {
                                        TicketId = newTicketRec.TicketId,
                                        TransferBatchId = newBatchRec.TransferBatchId,
                                        RawData = ticket.RawData,
                                        DeletedInd = false
                                    };
                                    session.Save(ticketRawData);
                                }
                                transaction.Commit();
							}
						}
					}
				}
			}
		}

        private static void getVoiceLogs(TicketGeneratorModel.TransferBatch batch)
        {
            if (batch.LogEntries == null)
                batch.LogEntries = new List<TicketGeneratorModel.LogEntry>();
            var pages = new List<RingCentral.CallLog>();
            var t = new RingCentral.CallLog(batch.RingCentralId);
            var dateTo = DateTime.Now.ToUniversalTime().AddHours(12);
            var dateFrom = dateTo.AddHours(-64);
            //var dateFrom = DateTime.Parse("8/1/2017 00:00:00");
            //var dateTo = DateTime.Parse("8/31/2017 23:59:59");
            var curPage = 1;
            var perPage = 500;
            t.DateFrom(dateFrom);
            t.DateTo(dateTo);
            t.WithRecording(null);
            t.PerPage(perPage);
            t.Page(curPage);
            t.Execute();
            pages.Add(t);
            while (t.data != null && t.data.navigation != null && t.data.navigation.nextPage != null && !string.IsNullOrWhiteSpace(t.data.navigation.nextPage.uri))
            {
                t = new RingCentral.CallLog(batch.RingCentralId);
                t.DateFrom(dateFrom);
                t.DateTo(dateTo);
                t.WithRecording(null);
                t.PerPage(perPage);
                t.Page(++curPage);
                t.Execute();
                pages.Add(t);
            }
            foreach (var page in pages)
            {
                if (page.data != null && page.data.records != null)
                {
                    foreach (var rec in page.data.records)
                    {
                        //ANALYZE ALL RECORDINGS AND VOICEMAILS
                        var voicemailList = new List<string>();
                        var recordingList = new List<string>();
                        var totalVoicemails = 0;
                        var totalRecordings = 0;
                        if (rec.message != null && rec.message.id != null)
                        {
                            totalVoicemails++;
                            voicemailList.Add(rec.message.id);
                        }
                        if (rec.recording != null && rec.recording.id != null)
                        {
                            totalRecordings++;
                            recordingList.Add(rec.recording.id);
                        }
                        if (rec.legs != null)
                        {
                            foreach (var leg in rec.legs)
                            {
                                if (leg.message != null && leg.message.id != null && !voicemailList.Any(x => x == leg.message.id))
                                {
                                    totalVoicemails++;
                                    voicemailList.Add(leg.message.id);
                                }
                                if (leg.recording != null && leg.recording.uri != null && !recordingList.Any(x => x == leg.recording.id))
                                {
                                    totalRecordings++;
                                    recordingList.Add(leg.recording.id);
                                }
                            }
                        }
                        //SERIALIZE IT
                        var jss = new JavaScriptSerializer();
                        var rawData = jss.Serialize(rec);
                        //CREATE REC
                        var newFile = new TicketGeneratorModel.LogEntry()
                        {
                            Type = "voice",
                            Id = rec.id,
                            RawData = rawData,
                            SaveAsFilename = generateCoreFileName(rec),
                            HasContent = (totalVoicemails > 0 || totalRecordings > 0)
                        };
                        getPreviousArchiveStatus(newFile, rec, batch.Rule, batch.AccountId);
                        batch.LogEntries.Add(newFile);
                    }
                }
            }
        }

        private static void getExtensions(TicketGeneratorModel.TransferBatch batch)
		{
			batch.Extensions = new List<string>();
            var pages = new List<RingCentral.ExtensionsGetter>();
            var curPage = 1;
            var perPage = 500;
            var t = new RingCentral.ExtensionsGetter(batch.RingCentralId);
            t.PerPage(perPage);
			t.Execute();
            pages.Add(t);
            while (t.data != null && t.data.navigation != null && t.data.navigation.nextPage != null && !string.IsNullOrWhiteSpace(t.data.navigation.nextPage.uri))
            {
                t = new RingCentral.ExtensionsGetter(batch.RingCentralId);
                t.Page(++curPage);
                t.PerPage(perPage);
                t.Execute();
                pages.Add(t);
            }

            foreach (var page in pages)
            {
                foreach (var rec in page.data.records)
                {
                    if (!string.IsNullOrEmpty(rec.id))
                    {
                        batch.Extensions.Add(rec.id);
                    }
                }
            }
        }

        private static void getFaxLogs(TicketGeneratorModel.TransferBatch batch)
        {
            if (batch.Extensions != null && batch.Extensions.Any())
            {
                foreach (var ext in batch.Extensions)
                {
                    getFaxLogsForExtension(batch, ext);
                }
            }
            else
            {
                getFaxLogsForExtension(batch, "");
            }
        }
        private static void getFaxLogsForExtension(TicketGeneratorModel.TransferBatch batch, string extensionId)
        {
            if (batch.LogEntries == null)
                batch.LogEntries = new List<TicketGeneratorModel.LogEntry>();
            var pages = new List<RingCentral.MessageStore>();
            var t = new RingCentral.MessageStore(batch.RingCentralId);
            var curPage = 1;
            var perPage = 500;
            var dateTo = DateTime.Now.ToUniversalTime().AddHours(12);
            var dateFrom = dateTo.AddHours(-64);
            if (!string.IsNullOrEmpty(extensionId))
                t.Extension(extensionId);
            t.MessageType("Fax");
            t.DateFrom(dateFrom);
            t.DateTo(dateTo);
            t.Page(curPage);
            t.PerPage(perPage);
            t.Execute();
            pages.Add(t);
            while (t.data != null && t.data.navigation != null && t.data.navigation.nextPage != null && !string.IsNullOrWhiteSpace(t.data.navigation.nextPage.uri))
            {
                t = new RingCentral.MessageStore(batch.RingCentralId);
                if (!string.IsNullOrEmpty(extensionId))
                    t.Extension(extensionId);
                t.MessageType("fax");
                t.DateFrom(dateFrom);
                t.DateTo(dateTo);
                t.Page(++curPage);
                t.PerPage(perPage);
                t.Execute();
                pages.Add(t);
            }
            foreach (var page in pages)
            {
                if (page.data != null && page.data.records != null)
                {
                    foreach (var rec in page.data.records)
                    {
                        //ANALYZE ALL ATTACHMENTS
                        var attachmentList = new List<string>();
                        var totalAttachments = 0;
                        if (rec.attachments != null)
                        {
                            foreach (var attachment in rec.attachments)
                            {
                                if (attachment.id != null && !attachmentList.Any(x => x == attachment.id))
                                {
                                    totalAttachments++;
                                    attachmentList.Add(attachment.id);
                                }
                            }
                        }
                        //SERIALIZE IT
                        var jss = new JavaScriptSerializer();
                        var rawData = jss.Serialize(rec);
                        //CREATE REC
                        var newFile = new TicketGeneratorModel.LogEntry()
                        {
                            Type = "fax",
                            Id = rec.id,
                            RawData = rawData,
                            SaveAsFilename = generateCoreFileName(rec),
                            HasContent = (totalAttachments > 0)
                        };
                        getPreviousArchiveStatus(newFile, rec, batch.Rule, batch.AccountId);
                        batch.LogEntries.Add(newFile);
                    }
                }
            }
        }

        private static void getSmsLogs(TicketGeneratorModel.TransferBatch batch)
        {
            if (batch.Extensions != null && batch.Extensions.Any())
            {
                foreach (var ext in batch.Extensions)
                {
                    getSmsLogsForExtension(batch, ext);
                }
            }
            else
            {
                getSmsLogsForExtension(batch, "");
            }
        }
        private static void getSmsLogsForExtension(TicketGeneratorModel.TransferBatch batch, string extensionId)
        {
            if (batch.LogEntries == null)
                batch.LogEntries = new List<TicketGeneratorModel.LogEntry>();
            var pages = new List<RingCentral.MessageStore>();
            var t = new RingCentral.MessageStore(batch.RingCentralId);
            var curPage = 1;
            var perPage = 500;
            var dateTo = DateTime.Now.ToUniversalTime().AddHours(12);
            var dateFrom = dateTo.AddHours(-64);
            if (batch.AccountId == 54)
            {
                dateFrom = dateTo.AddDays(-30);
            }
            if (!string.IsNullOrEmpty(extensionId))
                t.Extension(extensionId);
            t.MessageType("SMS");
            t.DateFrom(dateFrom);
            t.DateTo(dateTo);
            t.Page(curPage);
            t.PerPage(perPage);
            t.Execute();
            pages.Add(t);
            while (t.data != null && t.data.navigation != null && t.data.navigation.nextPage != null && !string.IsNullOrWhiteSpace(t.data.navigation.nextPage.uri))
            {
                var nextPage = t.data.navigation.nextPage.uri;
                t = new RingCentral.MessageStore(batch.RingCentralId);
                //if (!string.IsNullOrEmpty(extensionId))
                //    t.Extension(extensionId);
                t.NavTo(nextPage);
                //t.MessageType("sms");
                //t.DateFrom(dateFrom);
                //t.DateTo(dateTo);
                //t.Page(++curPage);
                //t.PerPage(perPage);
                t.Execute();
                pages.Add(t);
            }
            foreach (var page in pages)
            {
                if (page.data != null  && page.data.records != null)
                {
                    foreach (var rec in page.data.records)
                    {
                        //ANALYZE ALL ATTACHMENTS
                        var attachmentList = new List<string>();
                        var totalAttachments = 0;
                        if (rec.attachments != null)
                        {
                            foreach (var attachment in rec.attachments)
                            {
                                if (attachment.id != null && !attachmentList.Any(x => x == attachment.id))
                                {
                                    totalAttachments++;
                                    attachmentList.Add(attachment.id);
                                }
                            }
                        }
                        //SERIALIZE IT
                        var jss = new JavaScriptSerializer();
                        var rawData = jss.Serialize(rec);
                        //CREATE REC
                        var newFile = new TicketGeneratorModel.LogEntry()
                        {
                            Type = "sms",
                            Id = rec.id,
                            RawData = rawData,
                            SaveAsFilename = generateCoreFileName(rec),
                            HasContent = (totalAttachments > 0)
                        };
                        getPreviousArchiveStatus(newFile, rec, batch.Rule, batch.AccountId);
                        batch.LogEntries.Add(newFile);
                    }
                }
            }
        }

        public class TicketGeneratorModel
		{
			public DateTime Now;
			public List<TransferBatch> BatchesToRun;
			public class Rule
			{
				public int TransferRuleId;
				public int AccountId;
				public int FtpAccountId;
				public string Source;
				public string Destination;
				public int DestinationBoxAccountId;
				public int DestinationGoogleAccountId;
                public int DestinationFtpAccountId;
                public int DestinationAmazonAccountId;
                public string DestinationFolderId;
				public string DestinationFolderLabel;
                public string DestinationBucketName;
                public string DestinationPrefix;
                public bool PutInDatedSubfolder;
				public string Frequency;
				public string DayOf;
				public string TimeOfDay;
                public bool VoiceLogInd;
                public bool VoiceContentInd;
                public bool FaxLogInd;
                public bool FaxContentInd;
                public bool SmsLogInd;
                public bool SmsContentInd;
                public bool DeletedInd;
				public bool ActiveInd;
			}
			public class TransferBatch
			{
				public int TransferBatchId;
				public int AccountId;
				public string RingCentralId;
				public string Source;
				public string Destination;
				public string Frequency;
				public string TimeOfDay;
				public bool AccountIsInactive;
                public List<Ticket> Tickets;
				public Rule Rule;
				public List<string> Extensions;
				public List<LogEntry> LogEntries;
			}
			public class Ticket
			{
				public int TransferBatchId;
				public DateTime CreateDate;
				public string InitiatedBy;
				public string Destination;
				public int DestinationBoxAccountId;
				public int DestinationGoogleAccountId;
				public int DestinationFtpAccountId;
                public int DestinationAmazonAccountId;
                public string DestinationFolderId;
				public string DestinationFolderLabel;
                public string DestinationBucketName;
                public string DestinationPrefix;
                public bool PutInDatedSubfolder;
                public string CallId;
                public string MessageId;
                public string Type;
                public bool LogInd;
                public bool ContentInd;
                public bool ProcessingInd;
                public string RawData;
                public string SaveAsFilename;
            }
            public class LogEntry
            {
                public string Type;
                public string Id;
                public string RawData;
                public string SaveAsFilename;
                public bool HasContent;
                public bool Archived;
            }
        }


        private static void getPreviousArchiveStatus(TicketGeneratorModel.LogEntry logEntry, RingCentral.CallLog.CallLogData.Record rec, TicketGeneratorModel.Rule rule, int accountId)
		{
			using (ISessionFactory sessionFactory = CreateSessionFactory())
			{
				using (var session = sessionFactory.OpenSession())
				{
					var accountCrit = session.CreateCriteria<Account>();
					accountCrit.Add(Expression.Eq("AccountId", accountId));
					var accounts = accountCrit.List<Account>();
					if (accounts.Any())
					{
						var account = accounts.First();
						var ticketCrit = session.CreateCriteria<TicketRec>();
						ticketCrit.Add(Expression.Eq("CallId", rec.id));
						ticketCrit.Add(Expression.Eq("DeletedInd", false));
						var tickets = ticketCrit.List<TicketRec>();
                        if (tickets.Any())
                        {
                            logEntry.Archived = true;
                        }
					}
				}
			}
		}
		private static void getPreviousArchiveStatus(TicketGeneratorModel.LogEntry logEntry, RingCentral.MessageStore.MessageStoreData.Record rec, TicketGeneratorModel.Rule rule, int accountId)
		{
			using (ISessionFactory sessionFactory = CreateSessionFactory())
			{
				using (var session = sessionFactory.OpenSession())
				{
					var accountCrit = session.CreateCriteria<Account>();
					accountCrit.Add(Expression.Eq("AccountId", accountId));
					var accounts = accountCrit.List<Account>();
					if (accounts.Any())
					{
						var account = accounts.First();
						var ticketCrit = session.CreateCriteria<TicketRec>();
						ticketCrit.Add(Expression.Eq("MessageId", rec.id));
						ticketCrit.Add(Expression.Eq("DeletedInd", false));
						var tickets = ticketCrit.List<TicketRec>();
                        if (tickets.Any())
                        {
                            logEntry.Archived = true;
                        }
					}
				}
			}
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
            fileName += (DateTime.TryParse(rec.startTime, out time) ? time : DateTime.Now.ToUniversalTime()).ToString("yyyyMMdd_HHmm");
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

            DateTime time;
            fileName += (DateTime.TryParse(rec.creationTime, out time) ? time : DateTime.Now.ToUniversalTime()).ToString("yyyyMMdd_HHmm");
            fileName += ("_" + from + "_" + to + "_" + rec.direction + "_" + rec.messageStatus).Replace(" ", "-");

            return fileName;
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
				.BuildSessionFactory();
		}
#endregion

        #region Database Models

		private class Account
		{
			public virtual int AccountId { get; set; }
			public virtual string RingCentralId { get; set; }
			public virtual bool DeletedInd { get; set; }
			public virtual bool ActiveInd { get; set; }
			public virtual bool CancelledInd { get; set; }
			public virtual bool PaymentIsCurrentInd { get; set; }
		}
		private class AccountMap : ClassMap<Account>
		{
			public AccountMap()
			{
				Table("T_ACCOUNT");
				Id(x => x.AccountId).Column("AccountId");
                Map(x => x.RingCentralId);
                Map(x => x.DeletedInd);
                Map(x => x.ActiveInd);
                Map(x => x.CancelledInd);
                Map(x => x.PaymentIsCurrentInd);
            }
        }
		private class TicketRec
		{
			public virtual int TicketId { get; set; }
			public virtual int TransferBatchId { get; set; }
			public virtual DateTime CreateDate { get; set; }
			public virtual string InitiatedBy { get; set; }
			public virtual string Destination { get; set; }
			public virtual int DestinationBoxAccountId { get; set; }
			public virtual int DestinationGoogleAccountId { get; set; }
            public virtual int DestinationFtpAccountId { get; set; }
            public virtual int DestinationAmazonAccountId { get; set; }
            public virtual string DestinationFolderId { get; set; }
            public virtual string DestinationFolderLabel { get; set; }
            public virtual string DestinationBucketName { get; set; }
            public virtual string DestinationPrefix { get; set; }
            public virtual bool PutInDatedSubfolder { get; set; }
            public virtual string CallId { get; set; }
            public virtual string MessageId { get; set; }
            public virtual string Type { get; set; }
            public virtual bool LogInd { get; set; }
            public virtual bool ContentInd { get; set; }
            public virtual string SaveAsFilename { get; set; }
            public virtual bool DeletedInd { get; set; }
            public virtual bool ProcessingInd { get; set; }
		}
		private class TicketRecMap : ClassMap<TicketRec>
		{
			public TicketRecMap()
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
                Map(x => x.DestinationAmazonAccountId);
                Map(x => x.DestinationFolderId);
                Map(x => x.DestinationFolderLabel);
                Map(x => x.DestinationBucketName);
                Map(x => x.DestinationPrefix);
                Map(x => x.PutInDatedSubfolder);
				Map(x => x.CallId);
                Map(x => x.MessageId);
                Map(x => x.Type);
                Map(x => x.LogInd);
                Map(x => x.ContentInd);
                Map(x => x.SaveAsFilename);
                Map(x => x.DeletedInd);
				Map(x => x.ProcessingInd);
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

        public class TransferRuleRec
		{
			public virtual int TransferRuleId { get; set; }
			public virtual int AccountId { get; set; }
			public virtual string Source { get; set; }
			public virtual string Destination { get; set; }
			public virtual int DestinationBoxAccountId { get; set; }
			public virtual int DestinationGoogleAccountId { get; set; }
            public virtual int DestinationFtpAccountId { get; set; }
            public virtual int DestinationAmazonAccountId { get; set; }
            public virtual string DestinationFolderId { get; set; }
            public virtual string DestinationFolderLabel { get; set; }
            public virtual string DestinationBucketName { get; set; }
            public virtual string DestinationPrefix { get; set; }
            public virtual bool PutInDatedSubFolder { get; set; }
			public virtual string Frequency { get; set; }
			public virtual string DayOf { get; set; }
			public virtual string TimeOfDay { get; set; }
            public virtual bool VoiceLogInd { get; set; }
            public virtual bool VoiceContentInd { get; set; }
            public virtual bool FaxLogInd { get; set; }
            public virtual bool FaxContentInd { get; set; }
            public virtual bool SmsLogInd { get; set; }
            public virtual bool SmsContentInd { get; set; }
            public virtual bool DeletedInd { get; set; }
            public virtual bool ActiveInd { get; set; }
		}
		private class TransferRuleRecMap : ClassMap<TransferRuleRec>
		{
			public TransferRuleRecMap()
			{
				Table("T_TRANSFERRULE");
				Id(x => x.TransferRuleId).Column("TransferRuleId");
				Map(x => x.AccountId);
				Map(x => x.Source);
				Map(x => x.Destination);
				Map(x => x.DestinationBoxAccountId);
				Map(x => x.DestinationGoogleAccountId);
                Map(x => x.DestinationFtpAccountId);
                Map(x => x.DestinationAmazonAccountId);
                Map(x => x.DestinationFolderId);
                Map(x => x.DestinationFolderLabel);
                Map(x => x.DestinationBucketName);
                Map(x => x.DestinationPrefix);
                Map(x => x.PutInDatedSubFolder);
				Map(x => x.Frequency);
				Map(x => x.DayOf);
				Map(x => x.TimeOfDay);
                Map(x => x.VoiceLogInd);
                Map(x => x.VoiceContentInd);
                Map(x => x.FaxLogInd);
                Map(x => x.FaxContentInd);
                Map(x => x.SmsLogInd);
                Map(x => x.SmsContentInd);
                Map(x => x.DeletedInd);
                Map(x => x.ActiveInd);
			}
		}

		public class TransferBatchRec
		{
			public virtual int TransferBatchId { get; set; }
			public virtual int TransferRuleId { get; set; }
			public virtual int AccountId { get; set; }
			public virtual DateTime CreateDate { get; set; }
			public virtual bool QueuedInd { get; set; }
            public virtual int LogNumber { get; set; }
        }
        private class TransferBatchRecMap : ClassMap<TransferBatchRec>
		{
			public TransferBatchRecMap()
			{
				Table("T_TRANSFERBATCH");
				Id(x => x.TransferBatchId).Column("TransferBatchId");
				Map(x => x.TransferRuleId);
				Map(x => x.AccountId);
				Map(x => x.CreateDate);
                Map(x => x.QueuedInd);
                Map(x => x.LogNumber);
            }
        }
		private class TicketLogRec
		{
			public virtual int TicketLogId { get; set; }
			public virtual int TicketId { get; set; }
			public virtual int TransferBatchId { get; set; }
			public virtual DateTime? TicketLogStartDate { get; set; }
			public virtual DateTime? TicketLogStopDate { get; set; }
			public virtual bool ErrorInd { get; set; }
		}
		private class TicketLogRecMap : ClassMap<TicketLogRec>
		{
			public TicketLogRecMap()
			{
				Table("T_TICKETLOG");
				Id(x => x.TicketLogId).Column("TicketLogId");
				Map(x => x.TicketId);
				Map(x => x.TransferBatchId);
				Map(x => x.TicketLogStartDate);
				Map(x => x.TicketLogStopDate);
				Map(x => x.ErrorInd);
			}
		}

        #endregion

	}
}
