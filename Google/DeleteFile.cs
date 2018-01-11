using FluentNHibernate.Mapping;
using NHibernate;
using NHibernate.Criterion;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;

namespace GoogleActions
{

    public class DeleteFile : GoogleAction
    {
        private string fileId;
        public string ResponseFromServer;
        public DeleteFile(string username, int googleAccountId)
            : base(username, googleAccountId)
        {
        }
        public override void DoAction()
        {
            var url = "https://www.googleapis.com/drive/v3/files/" + fileId;
            var requestParams = new NameValueCollection();
            requestParams.Add("key", RingClone.AppConfig.Google.ClientId);
            requestParams.Add("access_token", AccessToken);
            var array = (from key in requestParams.AllKeys
                         from value in requestParams.GetValues(key)
                         select string.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(value)))
                .ToArray();
            var queryString = string.Join("&", array);
            url = url + "?" + queryString;
            WebRequest request = WebRequest.Create(url);
            request.Method = "DELETE";

            request.ContentType = "application/x-www-form-urlencoded";
            WebResponse response = request.GetResponse();
            Stream dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            ResponseFromServer = reader.ReadToEnd();
            reader.Close();
            dataStream.Close();
            response.Close();
        }
        public DeleteFile FileId(string fileId)
        {
            this.fileId = fileId;
            return this;
        }
    }
}
