using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using System;
using System.Data;
using System.Data.SqlClient;
using Dapper;

namespace ArchiveIndexer
{
    public class Functions
    {
        public static void ProcessQueueMessage([QueueTrigger("ringcentralindexerqueue")] string message, TextWriter log)
        {
            var model = JsonConvert.DeserializeObject<MessageModel>(message);
            if (model != null)
            {
                if (model.TicketId > 0)
                {
                    indexTicket(model.TicketId, model.OverrideCompleteInd, log);
                }
                if (model.TicketIds != null && model.TicketIds.Any())
                {
                    foreach (var ticketId in model.TicketIds)
                    {
                        indexTicket(ticketId, model.OverrideCompleteInd, log);
                    }
                }
            }
        }
        private static void indexTicket(int ticketId, bool overwriteCompleteInd, TextWriter log)
        {
            var startTime = DateTime.Now.ToUniversalTime();
            log.WriteLine("Indexing Ticket " + ticketId);
            IndexRec indexRec = null;
            IndexRawDataRec indexRawDataRec = null;
            Index indexRawData = null;
            IEnumerable<IndexRec> indexRecs = null;
            string direction = null;
            string action = null;
            string result = null;
            int duration = 0;
            string messageStatus = null;
            int faxPageCount = 0;
            using (IDbConnection db = new SqlConnection(ConnectionStringHelper.ConnectionString))
            {
                indexRecs = db.Query<IndexRec>("SELECT * FROM T_INDEX WHERE TicketId=@ticketId ORDER BY IndexId DESC", new { ticketId = ticketId });
                if (indexRecs.Any())
                    indexRec = indexRecs.First();
            }
            //CHECK FOR DUPS
            if (indexRecs.Count() > 1)
            {
                foreach (var indexRecToDelete in indexRecs.Where(x => x.IndexId != indexRec.IndexId))
                {
                    using (IDbConnection db = new SqlConnection(ConnectionStringHelper.ConnectionString))
                    {
                        db.Execute("DELETE FROM T_INDEXCALLER WHERE IndexId=@indexId", new { indexId = indexRecToDelete.IndexId });
                        db.Execute("DELETE FROM T_INDEXRAWDATA WHERE IndexRawDataId=@indexRawDataId", new { indexRawDataId = indexRecToDelete.IndexRawDataId });
                        db.Execute("DELETE FROM T_INDEXMESSAGE WHERE IndexMessageId=@indexMessageId", new { indexMessageId = indexRecToDelete.MessageId });
                        db.Execute("DELETE FROM T_INDEXFILEINFO WHERE IndexFileInfoId IN (SELECT IndexFileInfoId FROM T_INDEXFILE WHERE IndexId=@indexId)", new { indexId = indexRecToDelete.IndexId });
                        db.Execute("DELETE FROM T_INDEXFILE WHERE IndexId=@indexId", new { indexId = indexRecToDelete.IndexId });
                        db.Execute("DELETE FROM T_INDEX WHERE IndexId=@indexId", new { indexId = indexRecToDelete.IndexId });
                    }
                }
            }
            //GET RAWDATA FROM THE DATABASE
            using (IDbConnection db = new SqlConnection(ConnectionStringHelper.ConnectionString))
            {
                if (indexRec != null)
                {
                    if (indexRec.CompleteInd && !overwriteCompleteInd)
                        return;
                    indexRawDataRec = db.QuerySingle<IndexRawDataRec>("SELECT * FROM T_INDEXRAWDATA WHERE IndexRawDataId=@indexRawDataId", new { indexRawDataId = indexRec.IndexRawDataId });
                }
            }
            //DESERIALIZE RAW DATA
            if (indexRawDataRec != null)
            {
                indexRawData = JsonConvert.DeserializeObject<Index>(indexRawDataRec.RawData);
            }
            //PROCESS RAW DATA
            if (indexRawData != null)
            {
                if (!string.IsNullOrEmpty(indexRawData.CallId) && indexRawData.CallLog != null)
                {
                    indexCallers(indexRawData, indexRec);
                    direction = indexRawData.CallLog.direction;
                    action = indexRawData.CallLog.action;
                    result = indexRawData.CallLog.result;
                    duration = indexRawData.CallLog.duration;
                }
                if (!string.IsNullOrEmpty(indexRawData.MessageId) && indexRawData.MessageLog != null)
                {
                    indexMessage(indexRawData, indexRec);
                    direction = indexRawData.MessageLog.direction;
                }
                indexFiles(indexRawData, indexRec);
            }
            int elapsedTime = (int)Math.Floor((DateTime.Now.ToUniversalTime() - startTime).TotalMilliseconds);
            using (IDbConnection db = new SqlConnection(ConnectionStringHelper.ConnectionString))
            {
                db.Execute("UPDATE T_INDEX SET Direction=@direction,Action=@action,Result=@result,Duration=@duration,TimeElapsedForTransfer=@timeElapsedForTransfer,TimeElapsedForIndex=@timeElapsedForIndex,CompleteInd=1 WHERE IndexId=@indexId", new {
                    timeElapsedForTransfer = indexRawData.TimeElapsedForTransfer,
                    timeElapsedForIndex = elapsedTime,
                    direction = direction,
                    action = action,
                    result = result,
                    duration = duration,
                    indexId = indexRec.IndexId
                });
            }
        }
        public static void indexCallers(Index indexRawData, IndexRec indexRec)
        {
            var callers = new List<IndexCallerRec>();
            Func<RingCentral.CallLog.CallLogData.CallerInfo, IndexCallerRec> createCallerRec = (RingCentral.CallLog.CallLogData.CallerInfo callerInfo) =>
            {
                var callerRec = new IndexCallerRec();
                callerRec.IndexId = indexRec.IndexId;
                callerRec.ExtensionNumber = callerInfo.extensionNumber;
                callerRec.Location = callerInfo.location;
                callerRec.Name = callerInfo.name;
                callerRec.PhoneNumber = cleanNumber(callerInfo.phoneNumber);
                callerRec.ToInd = false;
                return callerRec;
            };
            if (indexRawData.CallLog.from != null)
            {
                var caller = createCallerRec(indexRawData.CallLog.from);
                callers.Add(caller);
            }
            if (indexRawData.CallLog.to != null)
            {
                var caller = createCallerRec(indexRawData.CallLog.to);
                caller.ToInd = true;
                callers.Add(caller);
            }
            if (indexRawData.CallLog.legs != null)
            {
                foreach (var leg in indexRawData.CallLog.legs)
                {
                    if (leg.from != null)
                    {
                        if (!callers.Any(x => (x.PhoneNumber == leg.from.phoneNumber || x.ExtensionNumber == leg.from.extensionNumber) && !x.ToInd))
                        {
                            var caller = createCallerRec(leg.from);
                            callers.Add(caller);
                        }
                        else if (callers.Any(x => (x.PhoneNumber == leg.from.phoneNumber || x.ExtensionNumber == leg.from.extensionNumber) && !x.ToInd))
                        {
                            var caller = callers.First(x => (x.PhoneNumber == leg.from.phoneNumber || x.ExtensionNumber == leg.from.extensionNumber) && !x.ToInd);
                            if (string.IsNullOrWhiteSpace(caller.Name) && !string.IsNullOrWhiteSpace(leg.from.name))
                                caller.Name = leg.from.name;
                        }
                    }
                    if (leg.to != null)
                    {
                        if (!callers.Any(x => (x.PhoneNumber == leg.to.phoneNumber || x.ExtensionNumber == leg.to.extensionNumber) && x.ToInd))
                        {
                            var caller = createCallerRec(leg.to);
                            caller.ToInd = true;
                            callers.Add(caller);
                        }
                        else if (callers.Any(x => (x.PhoneNumber == leg.to.phoneNumber || x.ExtensionNumber == leg.to.extensionNumber) && x.ToInd))
                        {
                            var caller = callers.First(x => (x.PhoneNumber == leg.to.phoneNumber || x.ExtensionNumber == leg.to.extensionNumber) && x.ToInd);
                            if (string.IsNullOrWhiteSpace(caller.Name) && !string.IsNullOrWhiteSpace(leg.to.name))
                                caller.Name = leg.to.name;
                        }
                    }
                }
            }
            //SAVE CALLERS TO THE DATABASE
            foreach (var caller in callers)
            {
                using (IDbConnection db = new SqlConnection(ConnectionStringHelper.ConnectionString))
                {
                    var existing = db.Query<IndexCallerRec>("SELECT * FROM T_INDEXCALLER WHERE IndexId=@indexId AND (PhoneNumber=@phoneNumber OR ExtensionNumber=@extensionNumber) AND ToInd=@toInd", new { indexId = indexRec.IndexId, phoneNumber = caller.PhoneNumber, extensionNumber = caller.ExtensionNumber, toInd = caller.ToInd });
                    if (!existing.Any())
                    {
                        db.Execute("INSERT INTO T_INDEXCALLER (IndexId, PhoneNumber, ExtensionNumber, Name, Location, ToInd) VALUES (@indexId,@phoneNumber,@extensionNumber,@name,@location,@toInd)", new { indexId = indexRec.IndexId, phoneNumber = caller.PhoneNumber, extensionNumber = caller.ExtensionNumber, name = caller.Name, location = caller.Location, toInd = caller.ToInd });
                    }
                    else
                    {
                        db.Execute("UPDATE T_INDEXCALLER SET name=@name,location=@location WHERE IndexCallerId=@indexCallerId", new { indexCallerId = existing.First().IndexCallerId, name = caller.Name, location = caller.Location });
                    }
                }
            }
        }
        public static void indexMessage(Index indexRawData, IndexRec indexRec)
        {
            var callers = new List<IndexCallerRec>();
            Func<RingCentral.MessageStore.MessageStoreData.CallerInfo, IndexCallerRec> createCallerRec = (RingCentral.MessageStore.MessageStoreData.CallerInfo callerInfo) =>
            {
                var callerRec = new IndexCallerRec();
                callerRec.IndexId = indexRec.IndexId;
                callerRec.ExtensionNumber = callerInfo.extensionNumber;
                callerRec.Location = callerInfo.location;
                callerRec.Name = callerInfo.name;
                callerRec.PhoneNumber = cleanNumber(callerInfo.phoneNumber);
                callerRec.ToInd = false;
                return callerRec;
            };
            if (indexRawData.MessageLog.from != null)
            {
                var caller = createCallerRec(indexRawData.MessageLog.from);
                callers.Add(caller);
            }
            if (indexRawData.MessageLog.to != null)
            {
                foreach (var to in indexRawData.MessageLog.to)
                {
                    var caller = createCallerRec(to);
                    caller.ToInd = true;
                    callers.Add(caller);
                }
            }
            //SAVE CALLERS TO THE DATABASE
            foreach (var caller in callers)
            {
                using (IDbConnection db = new SqlConnection(ConnectionStringHelper.ConnectionString))
                {
                    var existingCaller = db.Query<IndexCallerRec>("SELECT * FROM T_INDEXCALLER WHERE IndexId=@indexId AND (PhoneNumber=@phoneNumber OR ExtensionNumber=@extensionNumber) AND ToInd=@toInd", new { indexId = indexRec.IndexId, phoneNumber = caller.PhoneNumber, extensionNumber = caller.ExtensionNumber, toInd = caller.ToInd });
                    if (!existingCaller.Any())
                    {
                        db.Execute("INSERT INTO T_INDEXCALLER (IndexId, PhoneNumber, ExtensionNumber, Name, Location, ToInd) VALUES (@indexId,@phoneNumber,@extensionNumber,@name,@location,@toInd)", new { indexId = indexRec.IndexId, phoneNumber = caller.PhoneNumber, extensionNumber = caller.ExtensionNumber, name = caller.Name, location = caller.Location, toInd = caller.ToInd });
                    }
                    else
                    {
                        db.Execute("UPDATE T_INDEXCALLER SET name=@name,location=@location WHERE IndexCallerId=@indexCallerId", new { indexCallerId = existingCaller.First().IndexCallerId, name = caller.Name, location = caller.Location });
                    }
                }
            }

            //MESSAGE
            IndexMessageRec existing = null;
            if (indexRec.IndexMessageId > 0)
            {
                using (IDbConnection db = new SqlConnection(ConnectionStringHelper.ConnectionString))
                {
                    existing = db.QuerySingle<IndexMessageRec>("SELECT * FROM T_INDEXMESSAGE WHERE IndexMessageId=@indexMessageId", new { indexMessageId = indexRec.IndexMessageId });
                }
            }
            if (existing == null)
            {
                using (IDbConnection db = new SqlConnection(ConnectionStringHelper.ConnectionString))
                {
                    indexRec.IndexMessageId = db.Query<int>("INSERT INTO T_INDEXMESSAGE (CoverPageText,Subject,MessageStatus,FaxPageCount) VALUES (@coverPageText,@subject,@messageStatus,@faxPageCount); SELECT CAST(SCOPE_IDENTITY() AS int);", new { coverPageText = indexRawData.MessageLog.coverPageText, subject = indexRawData.MessageLog.subject, messageStatus = indexRawData.MessageLog.messageStatus, faxPageCount = indexRawData.MessageLog.faxPageCount }).Single();
                    db.Execute("UPDATE T_INDEX SET IndexMessageId=@indexMessageId WHERE IndexId=@indexId", new { indexMessageId = indexRec.IndexMessageId, indexId = indexRec.IndexId });
                }
            }
            else
            {
                using (IDbConnection db = new SqlConnection(ConnectionStringHelper.ConnectionString))
                {
                    db.Execute("UPDATE T_INDEXMESSAGE SET Subject=@subject,CoverPageText=@coverPageText,MessageStatus=@messageStatus,FaxPageCount=@faxPageCount WHERE IndexMessageId=@indexMessageId", new { indexMessageId = indexRec.IndexMessageId, coverPageText = indexRawData.MessageLog.coverPageText, subject = indexRawData.MessageLog.subject, messageStatus = indexRawData.MessageLog.messageStatus, faxPageCount = indexRawData.MessageLog.faxPageCount });
                }
            }
        }
        public static void indexFiles(Index indexRawData, IndexRec indexRec)
        {
            //DELETE ALL EXISTING FILE RECS
            using (IDbConnection db = new SqlConnection(ConnectionStringHelper.ConnectionString))
            {
                db.Execute("DELETE FROM T_INDEXFILEINFO WHERE IndexFileInfoId IN (SELECT IndexFileInfoId FROM T_INDEXFILE WHERE IndexId=@indexId)", new { indexId = indexRec.IndexId });
                db.Execute("DELETE FROM T_INDEXFILE WHERE IndexId=@indexId", new { indexId = indexRec.IndexId });
            }
            //ADD NEW ONES IN
            foreach (var file in indexRawData.Files)
            {
                using (IDbConnection db = new SqlConnection(ConnectionStringHelper.ConnectionString))
                {
                    //TODO: ADD FILEINFO
                    var fileInfoId = db.Query<int>("INSERT INTO T_INDEXFILEINFO (Filename,FileId,Folder,BucketName,Prefix) VALUES (@filename,@fileId,@folder,@bucketName,@prefix); SELECT CAST(SCOPE_IDENTITY() AS int);", new { filename = file.Filename, fileId = file.FileId, folder = file.Folder, bucketName = file.BucketName, prefix = file.Prefix }).Single();
                    db.Execute("INSERT INTO T_INDEXFILE (IndexId, LogInd, ContentInd, Destination, DestinationAccountId, ContentUri, NumberOfBytes, IndexFileInfoId) VALUES (@indexId, @logInd, @contentInd, @destination, @destinationAccountId, @contentUri, @numberOfBytes, @indexFileInfoId)", new { indexId = indexRec.IndexId, logInd = file.LogInd, contentInd = file.ContentInd, destination = file.Destination, destinationAccountId = file.DestinationAccountId, contentUri = file.ContentUri, numberOfBytes = file.NumberOfBytes, indexFileInfoId = fileInfoId });
                }
            }
        }
        public static string cleanNumber(string number)
        {
            if (!string.IsNullOrEmpty(number))
            {
                number = number.Replace(" ", "").Replace("(", "").Replace(")", "").Replace("-", "").Replace("+", "");
                if (number.Length == 11)
                    number = number.Substring(1, 10);
                number = number.Replace(" ", "");
            }
            return number;
        }
        public class MessageModel
        {
            public int TicketId { get; set; }
            public string CallId { get; set; }
            public string MessageId { get; set; }
            public IEnumerable<int> TicketIds { get; set; }
            public IEnumerable<string> CallIds { get; set; }
            public IEnumerable<string> MessageIds { get; set; }
            public bool OverrideCompleteInd { get; set; }
        }
        public class Index
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
            public List<IndexFile> Files;
            public class IndexFile
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
        #region Database Models
        public class IndexRec
        {
            public int IndexId;
            public int TicketId;
            public string CallId;
            public string MessageId;
            public int IndexRawDataId;
            public int IndexMessageId;
            public bool CompleteInd;
        }
        public class IndexRawDataRec
        {
            public int IndexRawDataId;
            public string RawData;
        }
        public class IndexCallerRec
        {
            public int IndexCallerId;
            public int IndexId;
            public string PhoneNumber;
            public string ExtensionNumber;
            public string Name;
            public string Location;
            public bool ToInd;
            public bool DeletedInd;
        }
        public class IndexMessageRec
        {
            public int IndexMessageId;
            public string Subject;
            public string CoverPageText;
        }
        public class IndexFileRec
        {
            public int IndexFileId;
            public int IndexId;
            public bool LogInd;
            public bool ContentInd;
            public int IndexFileInfoId;
            public string Destination;
            public int DestinationAccountId;
            public string ContentUri;
            public int NumberOfBytes;
            public bool DeletedInd;
        }
        public class IndexFileInfoRec
        {
            public int IndexFileInfoId;
            public string Filename;
            public string FileId;
            public string Folder;
            public string BucketName;
            public string Prefix;
        }
        #endregion
    }
}
