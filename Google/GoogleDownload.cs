using FluentNHibernate.Mapping;
using GoogleActions;
using NHibernate;
using NHibernate.Criterion;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;

namespace GoogleActions
{
    public class GoogleDownload : GoogleAction
    {
		public byte[] FileData;
		private string fileId;

		public GoogleDownload(string ringCentralId, int googleAccountId)
            : base(ringCentralId, googleAccountId)
        {
        }
        public override void DoAction()
        {
            var url = "https://www.googleapis.com/drive/v3/files/" + fileId + "?alt=media";
            WebRequest request = WebRequest.Create(url);
            request.Method = "GET";
            //request.ContentType = "application/x-www-form-urlencoded";
            request.Headers.Add("Authorization", "Bearer " + AccessToken);
            WebResponse response = request.GetResponse();
            using (Stream dataStream = response.GetResponseStream())
            using (MemoryStream ms = new MemoryStream())
            {
                int count = 0;
                do
                {
                    byte[] buf = new byte[1024];
                    count = dataStream.Read(buf, 0, 1024);
                    ms.Write(buf, 0, count);
                } while (dataStream.CanRead && count > 0);
                FileData = ms.ToArray();
            }
            response.Close();
		}
        public GoogleDownload FileId(string fileId)
        {
            this.fileId = fileId;
            return this;
        }
    }
}
