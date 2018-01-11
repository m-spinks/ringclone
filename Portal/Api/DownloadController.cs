using Dapper;
using FluentNHibernate.Mapping;
using Ionic.Zip;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using RingClone.Portal.Helpers;
using RingClone.Portal.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Caching;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Script.Serialization;

namespace RingClone.Portal.Api
{
    public class DownloadController : ApiController
    {
        [HttpPost]
        public bool Create(DownloadProcessorModel model)
        {
            if (model == null || model.Ids == null || !model.Ids.Any())
                return false;
            using (IDbConnection db = new SqlConnection(ConnectionStringHelper.ConnectionString))
            {
                var guid = Guid.NewGuid();
                if (string.IsNullOrWhiteSpace(model.DownloadId))
                    model.DownloadId = guid.ToString().Replace("{", "").Replace("}", "");
                if (string.IsNullOrWhiteSpace(model.Filename))
                    model.Filename = model.DownloadId;
                model.Filename = sanitizeFilename(model.Filename);
                if (!model.Filename.ToLower().EndsWith(".zip"))
                    model.Filename = model.Filename + ".zip";
                var accountRec = db.Query<AccountRec>("SELECT TOP 1 * FROM T_ACCOUNT WHERE RingCentralId=@ringCentralId ORDER BY AccountId", new { ringCentralId = User.Identity.RingCloneIdentity().RingCentralId }).First();
                var serializedModel = JsonConvert.SerializeObject(model);
                var downloadModelId = db.Query<int>("INSERT INTO T_DOWNLOADMODEL (Model) VALUES (@model); SELECT CAST(SCOPE_IDENTITY() AS int);", new { model = serializedModel }).Single();
                db.Query<string>("INSERT INTO T_DOWNLOAD (DownloadId,AccountId,DownloadDataId,DownloadModelId,CreateDate,Filename,Tooltip,[Percent]) VALUES (@downloadId,@accountId,@downloadDataId,@downloadModelId,@createDate,@filename,@tooltip,@percent); SELECT CAST(SCOPE_IDENTITY() AS varchar(50));", new { downloadId = model.DownloadId, accountId = accountRec.AccountId, downloadDataId = 0, downloadModelId = downloadModelId, createDate = DateTime.Now.ToUniversalTime(), filename = model.Filename, tooltip = "", percent = 0 }).Single();
            }
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConnectionStringHelper.StorageConnectionString);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue queue = queueClient.GetQueueReference("ringclonedownloadprocessorqueue");
            queue.CreateIfNotExists();
            CloudQueueMessage message = new CloudQueueMessage(model.DownloadId);
            queue.AddMessage(message);

            return true;
        }

        [HttpGet]
        public DownloadPollingModel Poll(bool refreshCache)
        {
            var model = new DownloadPollingModel();
            model.Downloads = new List<DownloadPollingModel.DownloadPollingItem>();
            var intervalPeriod = 600000;
            using (IDbConnection db = new SqlConnection(ConnectionStringHelper.ConnectionString))
            {
                var accountRec = db.Query<AccountRec>("SELECT TOP 1 * FROM T_ACCOUNT WHERE RingCentralId=@ringCentralId ORDER BY AccountId", new { ringCentralId = User.Identity.RingCloneIdentity().RingCentralId }).First();
                var downloadRecs = db.Query<DownloadRec>("SELECT TOP 10 * FROM T_DOWNLOAD WHERE AccountId=@accountId AND DeletedInd=0 ORDER BY CreateDate DESC", new { accountId = accountRec.AccountId }).OrderBy(x => x.CreateDate);
                foreach (var rec in downloadRecs)
                {
                    DownloadCache cacheModel = null;
                    var needToUpdate = false;
                    var isInCache = false;
                    var recentCaches = MemoryCache.Default.Where(x => x.Key == rec.DownloadId);
                    if (recentCaches.Any())
                    {
                        cacheModel = (DownloadCache)recentCaches.First().Value;
                        isInCache = true;
                        if (cacheModel.DownloadedInd != rec.DownloadedInd || cacheModel.ErrorInd != rec.ErrorInd || cacheModel.CompleteInd != rec.CompleteInd || cacheModel.SeenInd != rec.SeenInd || cacheModel.Percent != rec.Percent)
                        {
                            needToUpdate = true;
                        }
                    }
                    if (!isInCache)
                    {
                        cacheModel = new DownloadCache();
                        needToUpdate = true;
                    }
                    if (needToUpdate || refreshCache)
                    {
                        cacheModel.DownloadedInd = rec.DownloadedInd;
                        cacheModel.ErrorInd = rec.ErrorInd;
                        cacheModel.SeenInd = rec.SeenInd;
                        cacheModel.CompleteInd = rec.CompleteInd;
                        cacheModel.Percent = rec.Percent;
                        var policy = new CacheItemPolicy
                        {
                            AbsoluteExpiration = DateTimeOffset.UtcNow.AddMilliseconds(intervalPeriod)
                        };
                        MemoryCache.Default.Set(rec.DownloadId, cacheModel, policy);
                        var item = new DownloadPollingModel.DownloadPollingItem();
                        model.Downloads.Add(item);
                        item.CompleteInd = rec.CompleteInd;
                        item.CreateDate = rec.CreateDate;
                        item.DownloadedInd = rec.DownloadedInd;
                        item.DownloadId = rec.DownloadId;
                        item.ErrorInd = rec.ErrorInd;
                        item.Filename = rec.Filename;
                        item.Percent = rec.Percent;
                        item.Tooltip = getTooltip(rec);
                    }
                    if (rec.CompleteInd && !rec.DownloadedInd)
                        model.TotalNotDownloaded++;
                    if (rec.CompleteInd && !rec.SeenInd)
                        model.TotalNotSeen++;
                }
            }
            return model;
        }


        //    public async Task<HttpResponseMessage> Get2(string id)
        //    {
        //        var filenamesAndUrls = new Dictionary<string, string>
        //{
        //    { "README.md", "https://raw.githubusercontent.com/StephenClearyExamples/AsyncDynamicZip/master/README.md" },
        //    { ".gitignore", "https://raw.githubusercontent.com/StephenClearyExamples/AsyncDynamicZip/master/.gitignore" },
        //};


        //        DownloadDataRec dataRec = null;
        //        using (IDbConnection db = new SqlConnection(ConnectionStringHelper.ConnectionString))
        //        {
        //            var accountRec = db.Query<AccountRec>("SELECT TOP 1 * FROM T_ACCOUNT WHERE RingCentralId=@ringCentralId ORDER BY AccountId", new { ringCentralId = User.Identity.RingCloneIdentity().RingCentralId }).First();
        //            var downloadRec = db.Query<DownloadRec>("SELECT TOP 1 * FROM T_DOWNLOAD WHERE AccountId=@accountId AND DownloadId=@downloadId", new { accountId = accountRec.AccountId, downloadId = id }).First();
        //            dataRec = db.Query<DownloadDataRec>("SELECT TOP 1 * FROM T_DOWNLOADDATA WHERE DownloadDataId=@downloadDataId", new { downloadDataId = downloadRec.DownloadDataId }, null, true, 240).First();
        //            db.Execute("UPDATE T_DOWNLOAD SET DownloadedInd=1 WHERE AccountId=@accountId AND DownloadId=@downloadId", new { accountId = accountRec.AccountId, downloadId = id });
        //        }


        //        var archive = new MemoryStream();
        //        using (var zipStream = new ZipOutputStream(archive, leaveOpen: true))
        //        {
        //            foreach (var kvp in filenamesAndUrls)
        //            {
        //                zipStream.PutNextEntry(kvp.Key);
        //                using (var stream = await Client.GetStreamAsync(kvp.Value))
        //                    await stream.CopyToAsync(zipStream);
        //            }
        //        }

        //        archive.Position = 0;
        //        var result = new HttpResponseMessage(HttpStatusCode.OK)
        //        {
        //            Content = new StreamContent(archive)
        //        };
        //        result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        //        result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = "MyZipfile.zip" };
        //        return result;
        //    }
        public HttpResponseMessage Get(string id)
        {
            DownloadDataRec dataRec = null;
            string fileName;
            using (IDbConnection db = new SqlConnection(ConnectionStringHelper.ConnectionString))
            {
                var accountRec = db.Query<AccountRec>("SELECT TOP 1 * FROM T_ACCOUNT WHERE RingCentralId=@ringCentralId ORDER BY AccountId", new { ringCentralId = User.Identity.RingCloneIdentity().RingCentralId }).First();
                var downloadRec = db.Query<DownloadRec>("SELECT TOP 1 * FROM T_DOWNLOAD WHERE AccountId=@accountId AND DownloadId=@downloadId", new { accountId = accountRec.AccountId, downloadId = id }).First();
                fileName = downloadRec.Filename;
                dataRec = db.Query<DownloadDataRec>("SELECT TOP 1 * FROM T_DOWNLOADDATA WHERE DownloadDataId=@downloadDataId", new { downloadDataId = downloadRec.DownloadDataId }, null, true, 240).First();
                db.Execute("UPDATE T_DOWNLOAD SET DownloadedInd=1 WHERE AccountId=@accountId AND DownloadId=@downloadId", new { accountId = accountRec.AccountId, downloadId = id });
            }
            var pushStreamContent = new PushStreamContent((stream, content, context) =>
            {
                stream.Write(dataRec.Data, 0, dataRec.Data.Count());
                stream.Close();
            }, "application/zip");
            var result = new HttpResponseMessage(HttpStatusCode.OK) { Content = pushStreamContent };
            result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = fileName };
            return result;
        }

        private string getTooltip(DownloadRec rec)
        {
            var tooltip = "";
            if (rec.CompleteInd)
            {
                if (rec.ErrorInd)
                {
                    tooltip += "The download package<br/>was built with errors";
                }
                else
                {
                    tooltip += "The download package<br/>was built";
                }
            }
            else
            {
                tooltip += "The download package<br/>is building";
            }
            tooltip += "<br/><br/>Started on<br/>" + rec.CreateDate.ToString("yyyy/MM/dd hh:mm:ss tt");
            return tooltip;
        }
        private string sanitizeFilename(string filename)
        {
            var invalidFromSystem = System.IO.Path.GetInvalidFileNameChars();
            var invalidOverrides = new char[] { ' ', '_', '$', '#', '!', '@', '%', '^', '&', '*', '`' };
            var invalids = new char[invalidFromSystem.Length + invalidOverrides.Length];
            invalidFromSystem.CopyTo(invalids, 0);
            invalidOverrides.CopyTo(invalids, invalidFromSystem.Length);
            var newFilename = String.Join("_", filename.Split(invalids, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
            return newFilename;
        }

        #region Database Models
        private class AccountRec
        {
            public int AccountId;
            public string RingCentralId;
            public string RingCentralOwnerId;
            public bool DeletedInd;
            public bool ActiveInd;
            public bool CancelledInd;
        }
        private class DownloadRec
        {
            public string DownloadId;
            public int AccountId;
            public int DownloadDataId;
            public int DownloadModelId;
            public DateTime CreateDate;
            public string Filename;
            public string Tooltip;
            public int Percent;
            public bool ErrorInd;
            public bool CompleteInd;
            public bool DownloadedInd;
            public bool DeletedInd;
            public bool SeenInd;
        }
        private class DownloadModelRec
        {
            public int DownloadModelId;
            public string Model;
        }
        private class DownloadDataRec
        {
            public int DownloadDataId;
            public byte[] Data;
        }
        #endregion

        #region Other Models
        public class DownloadCache
        {
            public bool DownloadedInd;
            public bool ErrorInd;
            public int Percent;
            public bool SeenInd;
            public bool CompleteInd;
        }
        #endregion
    }
}
