using FluentNHibernate.Mapping;
using NHibernate;
using NHibernate.Criterion;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;

namespace GoogleActions
{

    public class GoogleFolder : GoogleAction
    {
        public GoogleFolderResponse Folders;
        public GoogleFolderResponse Files;
        public string FolderId
        {
            get
            {
                return folderId;
            }
        }
        private string folderId { get; set; }
        private string fileName;
        private string nextPageToken;
        public GoogleFolder(string username, int googleAccountId, string folderId)
            : base(username, googleAccountId)
        {
            if (string.IsNullOrWhiteSpace(folderId) || folderId == "0" || folderId == "undefined")
                folderId = "root";
            this.folderId = folderId;
        }
        public override void DoAction()
        {
            getFolders();
            getFiles();
        }
        public void getFolders()
        {
            var q = "mimeType='application/vnd.google-apps.folder' and '" + folderId + "' in parents";
            var googleEmailUrl = "https://www.googleapis.com/drive/v3/files";
            var requestParams = new NameValueCollection();
            requestParams.Add("key", RingClone.AppConfig.Google.ClientId);
            requestParams.Add("access_token", AccessToken);
            requestParams.Add("q", q);
            requestParams.Add("fields", "nextPageToken, files(id, name)");
            requestParams.Add("spaces", "drive");
            var array = (from key in requestParams.AllKeys
                            from value in requestParams.GetValues(key)
                            select string.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(value)))
                .ToArray();
            var queryString = string.Join("&", array);
            var url = googleEmailUrl + "?" + queryString;
            WebRequest request = WebRequest.Create(url);
            request.Method = "GET";

            request.ContentType = "application/x-www-form-urlencoded";
            WebResponse response = request.GetResponse();
            Stream dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string responseFromServer = reader.ReadToEnd();
            JavaScriptSerializer jss = new JavaScriptSerializer();
            Folders = jss.Deserialize<GoogleFolderResponse>(responseFromServer);
            reader.Close();
            dataStream.Close();
            response.Close();
        }
        public void getFiles()
        {
            var q = "mimeType != 'application/vnd.google-apps.folder' and '" + folderId + "' in parents";
            if (!string.IsNullOrWhiteSpace(fileName))
                q += " and name = '" + fileName + "'";
            var googleEmailUrl = "https://www.googleapis.com/drive/v3/files";
            var requestParams = new NameValueCollection();
            requestParams.Add("key", RingClone.AppConfig.Google.ClientId);
            requestParams.Add("access_token", AccessToken);
            requestParams.Add("q", q);
            requestParams.Add("fields", "nextPageToken, files(id, name, trashed, size, mimeType)");
            requestParams.Add("spaces", "drive");
            if (!string.IsNullOrWhiteSpace(nextPageToken))
                requestParams.Add("pageToken", nextPageToken);
            var array = (from key in requestParams.AllKeys
                            from value in requestParams.GetValues(key)
                            select string.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(value)))
                .ToArray();
            var queryString = string.Join("&", array);
            var url = googleEmailUrl + "?" + queryString;
            WebRequest request = WebRequest.Create(url);
            request.Method = "GET";

            request.ContentType = "application/x-www-form-urlencoded";
            WebResponse response = request.GetResponse();
            Stream dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string responseFromServer = reader.ReadToEnd();
            JavaScriptSerializer jss = new JavaScriptSerializer();
            Files = jss.Deserialize<GoogleFolderResponse>(responseFromServer);
            reader.Close();
            dataStream.Close();
            response.Close();
        }
        public GoogleFolder NextPageToken(string nextPageToken)
        {
            this.nextPageToken = nextPageToken;
            return this;
        }

        public GoogleFolder FileName(string fileName)
        {
            this.fileName = fileName;
            return this;
        }

        public class AccountRec
        {
            public virtual int AccountId { get; set; }
            public virtual string RingCentralId { get; set; }
            public virtual string RingCentralExtension { get; set; }
        }
        private class AccountRecMap : ClassMap<AccountRec>
        {
            public AccountRecMap()
            {
                Table("T_ACCOUNT");
                Id(x => x.AccountId).Column("AccountId");
                Map(x => x.RingCentralId);
                Map(x => x.RingCentralExtension);
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
                Table("T_BOXACCOUNT");
                Id(x => x.GoogleAccountId);
                Map(x => x.GoogleTokenId);
                Map(x => x.GoogleAccountName);
                Map(x => x.AccountId);
                Map(x => x.DeletedInd);
                Map(x => x.ActiveInd);
            }
        }

        public class GoogleFolderResponse
        {
            public string kind { get; set; }
            public string nextPageToken { get; set; }
            public List<File> files { get; set; }
            public class File
            {
                public string kind { get; set; }
                public string id { get; set; }
                public string name { get; set; }
                public string mimeType { get; set; }
                public string trashed { get; set; }
                public long size { get; set; }
            }
        }
    }
}
