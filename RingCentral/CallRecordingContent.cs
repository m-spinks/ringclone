using FluentNHibernate.Mapping;
using NHibernate;
using NHibernate.Criterion;
using RingCentral.Models;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace RingCentral
{
    public class CallRecordingContent : RingCentralAction
    {
		public string RecordingId;
        public byte[] data;

        public CallRecordingContent(string username, string recordingId)
			: base(username)
		{
			RecordingId = recordingId;
		}

        public override void DoAction()
        {
			var url = RingCentral.Config.ApiUrl + "/restapi/v1.0/account/~/recording/" + RecordingId + "/content";
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.Headers.Add("Authorization", "Bearer " + AccessToken);
            request.Accept = "application/json";
            WebResponse response = request.GetResponse();
            Stream dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
			data = new byte[dataStream.Length];
			dataStream.Read(data, 0, (int)dataStream.Length);
            reader.Close();
            dataStream.Close();
            response.Close();
        }
	}
}
