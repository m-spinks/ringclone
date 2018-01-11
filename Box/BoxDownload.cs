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
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace Box
{
    public class BoxDownload : BoxAction
    {
		private byte[] fileData;
		private string fileId;
        private string navigateToOrCreateSubFolder;

		public BoxDownload(string username, int boxAccountId)
            : base(username, boxAccountId)
        {
        }
        public override void DoAction()
        {
			try
			{
                var url = string.Format("https://api.box.com/2.0/files/{0}/content", fileId);
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
                    fileData = ms.ToArray();
                }
                response.Close();
            }
            catch (WebException ex)
			{
				if (ex.HResult == 401 || ex.Message.Contains("401") || ex.Message.Contains("Unauthorized"))
					throw ex;
				else
					ResultException = ex;
			}
			catch (Exception ex)
			{
                if (ex.Message == "Unauthorized")
                    throw ex;
                else
    				ResultException = ex;
			}
		}

		public BoxDownload FileId(string fileId)
		{
			this.fileId = fileId;
			return this;
		}
		public byte[] FileData()
		{
			return this.fileData;
		}

	}
}
