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
    public class ArchiveController : ApiController
    {
        [HttpGet]
        public HttpResponseMessage File(int id)
        {
            AccountRec accountRec;
            IndexFileRec fileRec;
            IndexFileInfoRec fileInfoRec;
            byte[] fileData = null;
            using (IDbConnection db = new SqlConnection(ConnectionStringHelper.ConnectionString))
            {
                accountRec = db.Query<AccountRec>("SELECT TOP 1 * FROM T_ACCOUNT WHERE RingCentralId=@ringCentralId ORDER BY AccountId", new { ringCentralId = User.Identity.RingCloneIdentity().RingCentralId }).First();
                fileRec = db.QueryFirst<IndexFileRec>("SELECT * FROM T_INDEXFILE INNER JOIN T_INDEX ON T_INDEXFILE.IndexId = T_INDEX.IndexId WHERE OwnerId=@ownerId AND T_INDEXFILE.IndexFileId=@indexFileId", new { ownerId = accountRec.RingCentralOwnerId, indexFileId = id });
                fileInfoRec = db.QueryFirst<IndexFileInfoRec>("SELECT * FROM T_INDEXFILEINFO WHERE IndexFileInfoId = @fileInfoId", new { fileInfoId = fileRec.IndexFileInfoId });
            }

            if (accountRec != null && fileRec != null && fileInfoRec != null)
            {
                if (fileRec.Destination.ToLower() == "amazon")
                {
                    var downloader = new AmazonActions.AmazonDownload(accountRec.RingCentralId, fileRec.DestinationAccountId);
                    downloader.BucketName(fileInfoRec.BucketName);
                    downloader.Key(fileInfoRec.Prefix + fileInfoRec.Filename);
                    downloader.Execute();
                    if (downloader.ResultException != null)
                    {
                        throw downloader.ResultException;
                    }
                    else if (downloader.FileData() == null || downloader.FileData().Length == 0)
                    {
                        throw new Exception(string.Format("Unknown error when downloading file {0} from archive controller", id));
                    }
                    else
                    {
                        fileData = downloader.FileData();
                    }
                }
                else if (fileRec.Destination.ToLower() == "google")
                {
                    var downloader = new GoogleActions.GoogleDownload(accountRec.RingCentralId, fileRec.DestinationAccountId);
                    downloader.FileId(fileInfoRec.FileId);
                    downloader.Execute();
                    if (downloader.ResultException != null)
                    {
                        throw downloader.ResultException;
                    }
                    else if (downloader.FileData == null || downloader.FileData.Length == 0)
                    {
                        throw new Exception(string.Format("Unknown error when downloading file {0} from archive controller", id));
                    }
                    else
                    {
                        fileData = downloader.FileData;
                    }
                }
                else if (fileRec.Destination.ToLower() == "box")
                {
                    var downloader = new Box.BoxDownload(accountRec.RingCentralId, fileRec.DestinationAccountId);
                    downloader.FileId(fileInfoRec.FileId);
                    downloader.Execute();
                    if (downloader.ResultException != null)
                    {
                        throw downloader.ResultException;
                    }
                    else if (downloader.FileData() == null || downloader.FileData().Length == 0)
                    {
                        throw new Exception(string.Format("Unknown error when downloading file {0} from archive controller", id));
                    }
                    else
                    {
                        fileData = downloader.FileData();
                    }
                }
                if (fileData != null)
                {
                    if (fileRec.ContentInd)
                    {
                        if (fileInfoRec.Filename.EndsWith("mp3"))
                        {
                            var pushStreamContent = new PushStreamContent((stream, content, context) =>
                            {
                                stream.Write(fileData, 0, fileData.Count());
                                stream.Close();
                            }, "audio/mpeg");
                            var result = new HttpResponseMessage(HttpStatusCode.OK) { Content = pushStreamContent };
                            result.Content.Headers.ContentType = new MediaTypeHeaderValue("audio/mpeg");
                            result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = fileInfoRec.Filename };
                            return result;
                        }
                        else if (fileInfoRec.Filename.EndsWith("jpg"))
                        {
                            var pushStreamContent = new PushStreamContent((stream, content, context) =>
                            {
                                stream.Write(fileData, 0, fileData.Count());
                                stream.Close();
                            }, "image/jpeg");
                            var result = new HttpResponseMessage(HttpStatusCode.OK) { Content = pushStreamContent };
                            result.Content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
                            result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = fileInfoRec.Filename };
                            return result;
                        }
                        else if (fileInfoRec.Filename.EndsWith("gif"))
                        {
                            var pushStreamContent = new PushStreamContent((stream, content, context) =>
                            {
                                stream.Write(fileData, 0, fileData.Count());
                                stream.Close();
                            }, "image/gif");
                            var result = new HttpResponseMessage(HttpStatusCode.OK) { Content = pushStreamContent };
                            result.Content.Headers.ContentType = new MediaTypeHeaderValue("image/gif");
                            result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = fileInfoRec.Filename };
                            return result;
                        }
                        else if (fileInfoRec.Filename.EndsWith("pdf"))
                        {
                            var pushStreamContent = new PushStreamContent((stream, content, context) =>
                            {
                                stream.Write(fileData, 0, fileData.Count());
                                stream.Close();
                            }, "application/pdf");
                            var result = new HttpResponseMessage(HttpStatusCode.OK) { Content = pushStreamContent };
                            result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                            result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = fileInfoRec.Filename };
                            return result;
                        }
                        else
                        {
                            var pushStreamContent = new PushStreamContent((stream, content, context) =>
                            {
                                stream.Write(fileData, 0, fileData.Count());
                                stream.Close();
                            });
                            var result = new HttpResponseMessage(HttpStatusCode.OK) { Content = pushStreamContent };
                            result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = fileInfoRec.Filename };
                            return result;
                        }
                    }
                    else
                    {
                        var logText = System.Text.Encoding.UTF8.GetString(fileData);
                        var result = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(logText) };
                        result.Content = new StringContent(logText, System.Text.Encoding.UTF8, "text/plain");
                        result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = fileInfoRec.Filename };
                        return result;
                    }
                }
            }
            return null;
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
            public int TotalRecs;
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
