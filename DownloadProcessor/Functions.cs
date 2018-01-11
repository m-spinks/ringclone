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
using Ionic.Zip;
using GoogleActions;

namespace DownloadProcessor
{
    public class Functions
    {
        public static void ProcessQueueMessage([QueueTrigger("ringclonedownloadprocessorqueue")] string message, TextWriter log)
        {
            var downloadId = message;
            DownloadRec downloadRec = null;
            DownloadModelRec downloadModelRec = null;
            DownloadProcessorModel model = null;
            IEnumerable<IndexFileRec> dbFileRecs = null;
            IEnumerable<IndexFileInfoRec> dbFileInfoRecs = null;
            IEnumerable<IndexRec> dbIndexRecs = null;
            AccountRec accountRec = null;
            var filesToPutInZip = new List<FileToPutInZip>();
            var totalSteps = 0;
            var currentStep = 0;
            var percent = 0;
            var downloadError = false;
            try
            {
                //INITIALIZE STATUS
                using (IDbConnection db = new SqlConnection(ConnectionStringHelper.ConnectionString))
                {
                    db.Execute("UPDATE T_DOWNLOAD SET [Percent]=0 WHERE DownloadId=@downloadId", new { downloadId = downloadId });
                }
                //GET DOWNLOAD MODEL AND ACCOUNT INFO
                using (IDbConnection db = new SqlConnection(ConnectionStringHelper.ConnectionString))
                {
                    downloadRec = db.Query<DownloadRec>("SELECT TOP 1 * FROM T_DOWNLOAD WHERE DownloadId=@downloadId ORDER BY CreateDate DESC", new { downloadId = downloadId }).First();
                    downloadModelRec = db.Query<DownloadModelRec>("SELECT TOP 1 * FROM T_DOWNLOADMODEL WHERE DownloadModelId=@downloadModelId", new { downloadModelId = downloadRec.DownloadModelId }).First();
                    model = JsonConvert.DeserializeObject<DownloadProcessorModel>(downloadModelRec.Model);
                    accountRec = db.Query<AccountRec>("SELECT TOP 1 * FROM T_ACCOUNT WHERE AccountId=@accountId ORDER BY AccountId", new { AccountId = downloadRec.AccountId }).First();
                }
                //GET INDEXED DATA
                if (model.Type == "voice")
                {
                    using (IDbConnection db = new SqlConnection(ConnectionStringHelper.ConnectionString))
                    {
                        dbIndexRecs = db.Query<IndexRec>("SELECT * FROM T_INDEX WHERE OwnerId=@ownerId AND Type=@type AND CallId IN @callIds AND CompleteInd=1 ORDER BY IndexId DESC", new { ownerId = accountRec.RingCentralOwnerId, type = model.Type, callIds = model.Ids });
                        dbFileRecs = db.Query<IndexFileRec>("SELECT * FROM T_INDEXFILE WHERE IndexId IN @indexRecs", new { indexRecs = dbIndexRecs.Select(x => x.IndexId) });
                        dbFileInfoRecs = db.Query<IndexFileInfoRec>("SELECT * FROM T_INDEXFILEINFO WHERE IndexFileInfoId IN @fileInfoIds", new { fileInfoIds = dbFileRecs.Select(x => x.IndexFileInfoId) });
                    }
                }
                else if (model.Type == "fax" || model.Type == "sms")
                {
                    using (IDbConnection db = new SqlConnection(ConnectionStringHelper.ConnectionString))
                    {
                        dbIndexRecs = db.Query<IndexRec>("SELECT * FROM T_INDEX WHERE OwnerId=@ownerId AND Type=@type AND MessageId IN @messageIds AND CompleteInd=1 ORDER BY IndexId DESC", new { ownerId = accountRec.RingCentralOwnerId, type = model.Type, messageIds = model.Ids });
                        dbFileRecs = db.Query<IndexFileRec>("SELECT * FROM T_INDEXFILE WHERE IndexId IN @indexRecs", new { indexRecs = dbIndexRecs.Select(x => x.IndexId) });
                        dbFileInfoRecs = db.Query<IndexFileInfoRec>("SELECT * FROM T_INDEXFILEINFO WHERE IndexFileInfoId IN @fileInfoIds", new { fileInfoIds = dbFileRecs.Select(x => x.IndexFileInfoId) });
                    }
                }                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      
                //"DOWNLOAD" DATA FROM DEST SERVERS
                if (dbIndexRecs != null && dbIndexRecs.Any() && dbFileRecs != null && dbFileRecs.Any())
                {
                    if (!model.LogInd)
                        dbFileRecs = dbFileRecs.Where(x => !x.LogInd);
                    if (!model.ContentInd)
                        dbFileRecs = dbFileRecs.Where(x => !x.ContentInd);
                    totalSteps = dbFileRecs.Count() + 1;
                    using (var memoryStream = new MemoryStream())
                    {
                        using (var zipStream = new ZipOutputStream(memoryStream))
                        {
                            foreach (var fileRec in dbFileRecs)
                            {
                                currentStep++;
                                percent = (int)Math.Floor(((double)currentStep / (double)totalSteps) * 100);
                                using (IDbConnection db = new SqlConnection(ConnectionStringHelper.ConnectionString))
                                {
                                    db.Execute("UPDATE T_DOWNLOAD SET [Percent]=@percent WHERE DownloadId=@downloadId", new { percent = percent, downloadId = downloadId });
                                }
                                if (fileRec.Destination.ToLower() == "amazon")
                                {
                                    var fileInfoRec = dbFileInfoRecs.First(x => x.IndexFileInfoId == fileRec.IndexFileInfoId);
                                    var downloader = new AmazonActions.AmazonDownload(accountRec.RingCentralId, fileRec.DestinationAccountId);
                                    downloader.BucketName(fileInfoRec.BucketName);
                                    downloader.Key(fileInfoRec.Prefix + fileInfoRec.Filename);
                                    downloader.Execute();
                                    if (downloader.ResultException != null)
                                    {
                                        using (IDbConnection db = new SqlConnection(ConnectionStringHelper.ConnectionString))
                                        {
                                            db.Execute("INSERT INTO T_DOWNLOADERROR (DownloadId,ErrorDate,[Error]) VALUES (@downloadId,@errorDate,@error)", new { downloadId = downloadRec.DownloadId, errorDate = DateTime.Now.ToUniversalTime(), error = downloader.ResultException.Message });
                                        }
                                        downloadError = true;
                                        var fileToPutInZip = new FileToPutInZip();
                                        fileToPutInZip.FileName = fileInfoRec.Filename + "_error.log";
                                        fileToPutInZip.FileData = Encoding.ASCII.GetBytes("An error occurred when downloading this file. Please contact RingClone support for assistance.");
                                        filesToPutInZip.Add(fileToPutInZip);
                                    }
                                    else if (downloader.FileData() == null || downloader.FileData().Length == 0)
                                    {
                                        using (IDbConnection db = new SqlConnection(ConnectionStringHelper.ConnectionString))
                                        {
                                            db.Execute("INSERT INTO T_DOWNLOADERROR (DownloadId,ErrorDate,[Error]) VALUES (@downloadId,@errorDate,@error)", new { downloadId = downloadRec.DownloadId, errorDate = DateTime.Now.ToUniversalTime(), error = "No content downloaded" });
                                        }
                                        downloadError = true;
                                        var fileToPutInZip = new FileToPutInZip();
                                        fileToPutInZip.FileName = fileInfoRec.Filename + "_error.log";
                                        fileToPutInZip.FileData = Encoding.ASCII.GetBytes("An error occurred when downloading this file. Please contact RingClone support for assistance.");
                                        filesToPutInZip.Add(fileToPutInZip);
                                    }
                                    else
                                    {
                                        var fileToPutInZip = new FileToPutInZip();
                                        fileToPutInZip.FileName = fileInfoRec.Filename;
                                        fileToPutInZip.FileData = downloader.FileData();
                                        filesToPutInZip.Add(fileToPutInZip);
                                    }
                                }
                                else if (fileRec.Destination.ToLower() == "google")
                                {
                                    var fileInfoRec = dbFileInfoRecs.First(x => x.IndexFileInfoId == fileRec.IndexFileInfoId);
                                    var downloader = new GoogleActions.GoogleDownload(accountRec.RingCentralId, fileRec.DestinationAccountId);
                                    downloader.FileId(fileInfoRec.FileId);
                                    downloader.Execute();
                                    if (downloader.ResultException != null)
                                    {
                                        using (IDbConnection db = new SqlConnection(ConnectionStringHelper.ConnectionString))
                                        {
                                            db.Execute("INSERT INTO T_DOWNLOADERROR (DownloadId,ErrorDate,[Error]) VALUES (@downloadId,@errorDate,@error)", new { downloadId = downloadRec.DownloadId, errorDate = DateTime.Now.ToUniversalTime(), error = downloader.ResultException.Message });
                                        }
                                        downloadError = true;
                                    }
                                    else if (downloader.FileData == null || downloader.FileData.Length == 0)
                                    {
                                        using (IDbConnection db = new SqlConnection(ConnectionStringHelper.ConnectionString))
                                        {
                                            db.Execute("INSERT INTO T_DOWNLOADERROR (DownloadId,ErrorDate,[Error]) VALUES (@downloadId,@errorDate,@error)", new { downloadId = downloadRec.DownloadId, errorDate = DateTime.Now.ToUniversalTime(), error = "No content downloaded" });
                                        }
                                        downloadError = true;
                                    }
                                    else
                                    {
                                        var fileToPutInZip = new FileToPutInZip();
                                        fileToPutInZip.FileName = fileInfoRec.Filename;
                                        fileToPutInZip.FileData = downloader.FileData;
                                        filesToPutInZip.Add(fileToPutInZip);
                                    }
                                }
                                else if (fileRec.Destination.ToLower() == "box")
                                {
                                    var fileInfoRec = dbFileInfoRecs.First(x => x.IndexFileInfoId == fileRec.IndexFileInfoId);
                                    var downloader = new Box.BoxDownload(accountRec.RingCentralId, fileRec.DestinationAccountId);
                                    downloader.FileId(fileInfoRec.FileId);
                                    downloader.Execute();
                                    if (downloader.ResultException != null)
                                    {
                                        using (IDbConnection db = new SqlConnection(ConnectionStringHelper.ConnectionString))
                                        {
                                            db.Execute("INSERT INTO T_DOWNLOADERROR (DownloadId,ErrorDate,[Error]) VALUES (@downloadId,@errorDate,@error)", new { downloadId = downloadRec.DownloadId, errorDate = DateTime.Now.ToUniversalTime(), error = downloader.ResultException.Message });
                                        }
                                        downloadError = true;
                                    }
                                    else if (downloader.FileData() == null || downloader.FileData().Length == 0)
                                    {
                                        using (IDbConnection db = new SqlConnection(ConnectionStringHelper.ConnectionString))
                                        {
                                            db.Execute("INSERT INTO T_DOWNLOADERROR (DownloadId,ErrorDate,[Error]) VALUES (@downloadId,@errorDate,@error)", new { downloadId = downloadRec.DownloadId, errorDate = DateTime.Now.ToUniversalTime(), error = "No content downloaded" });
                                        }
                                        downloadError = true;
                                    }
                                    else
                                    {
                                        var fileToPutInZip = new FileToPutInZip();
                                        fileToPutInZip.FileName = fileInfoRec.Filename;
                                        fileToPutInZip.FileData = downloader.FileData();
                                        filesToPutInZip.Add(fileToPutInZip);
                                    }
                                }
                            }
                        }
                    }
                }
                //PACKAGE THE DOWNLOAD DATA INTO A ZIP
                MemoryStream memoryStreamForZip = new MemoryStream();
                using (ZipFile zip = new ZipFile())
                {
                    foreach (var fileToPutInZip in filesToPutInZip)
                    {
                        var fileNameToDownload = fileToPutInZip.FileName;
                        while (zip.ContainsEntry(fileNameToDownload))
                        {
                            fileNameToDownload = createNextFileName(fileNameToDownload);
                        }
                        zip.AddEntry(fileNameToDownload, fileToPutInZip.FileData);
                    }
                    zip.Save(memoryStreamForZip);
                }
                //SAVE ZIP TO DATABASE AND FLAG RECORD
                memoryStreamForZip.Seek(0, SeekOrigin.Begin);
                var finalData = new byte[memoryStreamForZip.Length];
                memoryStreamForZip.Read(finalData, 0, (int)memoryStreamForZip.Length);
                using (IDbConnection db = new SqlConnection(ConnectionStringHelper.ConnectionString))
                {
                    db.Execute("DELETE FROM T_DOWNLOADDATA WHERE DownloadDataId=@downloadDataId", new { downloadDataId = downloadRec.DownloadDataId });
                    var newDownloadDataId = db.Query<int>("INSERT INTO T_DOWNLOADDATA ([Data]) VALUES (@data); SELECT CAST(SCOPE_IDENTITY() AS int);", new { data = finalData }, null, true, 240).Single();
                    db.Execute("UPDATE T_DOWNLOAD SET downloadDataId=@downloadDataId,CompleteInd=1,[Percent]=100,ErrorInd=@errorInd WHERE DownloadId=@downloadId", new { downloadDataId = newDownloadDataId, downloadId = downloadId, errorInd = downloadError });
                }
            }
            catch (Exception ex)
            {
                var error = ex.Message;
                if (ex.InnerException != null)
                    error += " --> " + ex.InnerException.Message;
                using (IDbConnection db = new SqlConnection(ConnectionStringHelper.ConnectionString))
                {
                    db.Execute("INSERT INTO T_DOWNLOADERROR (DownloadId,ErrorDate,[Error]) VALUES (@downloadId,@errorDate,@error)", new { downloadId = downloadRec.DownloadId, errorDate = DateTime.Now.ToUniversalTime(), error = error });
                }
                throw ex;
            }
        }

        private static string createNextFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "2";
            var pieces = fileName.Split('.');
            string piece;
            if (pieces.Length == 1)
                piece = pieces[0];
            else
                piece = pieces[pieces.Length - 2];
            var smallPieces = piece.Split('_');
            int curNumber;
            if (!int.TryParse(smallPieces.Last(), out curNumber))
            {
                curNumber = 1;
                Array.Resize(ref smallPieces, smallPieces.Length + 1);
            }
            smallPieces[smallPieces.Length - 1] = (++curNumber).ToString();
            if (pieces.Length == 1)
                pieces[0] = string.Join("_", smallPieces);
            else
                pieces[pieces.Length - 2] = string.Join("_", smallPieces);
            return string.Join(".", pieces);
        }

        #region Database Models
        private class AccountRec
        {
            public int AccountId { get; set; }
            public string RingCentralId { get; set; }
            public string RingCentralOwnerId { get; set; }
            public bool DeletedInd { get; set; }
            public bool ActiveInd { get; set; }
            public bool CancelledInd { get; set; }
        }
        private class DownloadRec
        {
            public string DownloadId { get; set; }
            public int AccountId { get; set; }
            public int DownloadDataId { get; set; }
            public int DownloadModelId { get; set; }
            public DateTime CreateDate { get; set; }
            public int Percent { get; set; }
            public bool ErrorInd { get; set; }
            public bool CompleteInd { get; set; }
            public bool DeletedInd { get; set; }
        }
        private class DownloadModelRec
        {
            public int DownloadModelId { get; set; }
            public string Model { get; set; }
        }
        private class IndexRawDataRec
        {
            public int IndexRawDataId { get; set; }
            public string RawData { get; set; }
        }
        private class DownloadErrorRec
        {
            public int DownloadErrorId { get; set; }
            public string DownloadId { get; set; }
            public DateTime ErrorDate { get; set; }
            public string Error { get; set; }
        }
        public class IndexRec
        {
            public int IndexId;
            public int TicketId;
            public string Type;
            public string CallId;
            public string MessageId;
            public int IndexRawDataId;
            public int IndexMessageId;
            public bool CompleteInd;
            public bool DeletedInd;
            public string Direction;
            public string Action;
            public string Result;
            public int Duration;
            public DateTime CallTime;
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

        #region Other Models
        public class DownloadProcessorModel
        {
            public string Type { get; set; }
            public bool LogInd { get; set; }
            public bool ContentInd { get; set; }
            public string Filename { get; set; }
            public string DownloadId { get; set; }
            public List<string> Ids { get; set; }
        }
        public class FileToPutInZip
        {
            public string FileName;
            public byte[] FileData;
        }
        #endregion
    }
}
