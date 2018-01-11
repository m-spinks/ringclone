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
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace RingCentral
{
    public class MessageContent : RingCentralAction
    {
		public string MessageId;
        public MessageMetaData messageMetaData;
        public byte[] data;
        public CallLogData callLogData;

        public MessageContent(string username, string messageId)
			: base(username)
		{
			MessageId = messageId;
		}

        public override void DoAction()
        {
			//getTestFile();
			//getMetaData();
            getCallFromCallLog();
			getAttachments();
        }

		private void getTestFile()
		{
			var url = "http://www.northtechnologies.com/images/political-discussion.mp3";
			var request = (HttpWebRequest)WebRequest.Create(url);
			request.Method = "GET";
			request.Headers.Add("Authorization", "Bearer " + AccessToken);
			request.Accept = "application/json";
			WebResponse response = request.GetResponse();
			var dataStream = new MemoryStream();
			using (Stream responseStream = request.GetResponse().GetResponseStream())
			{
				byte[] buffer = new byte[0x1000];
				int bytes;
				while ((bytes = responseStream.Read(buffer, 0, buffer.Length)) > 0)
				{
					dataStream.Write(buffer, 0, bytes);
				}
			}
			data = new byte[dataStream.Length];
			dataStream.Seek(0, SeekOrigin.Begin);
			dataStream.Read(data, 0, (int)dataStream.Length);
			//Stream dataStream = response.GetResponseStream();
			//StreamReader reader = new StreamReader(dataStream);
			//data = new byte[];
			//dataStream.Read(data, 0, (int)dataStream.Length);
			//reader.Close();
			//dataStream.Close();
			response.Close();
		}
        private void getCallFromCallLog()
        {
            var rootUrl = RingCentral.Config.ApiUrl + "/restapi/v1.0/account/~/extension/~/call-log/" + MessageId;
            var url = rootUrl;// +"?" + queryString;
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.Headers.Add("Authorization", "Bearer " + AccessToken);
            request.Accept = "application/json";
            WebResponse response = request.GetResponse();
            Stream dataStream = response.GetResponseStream();
            //StreamReader reader = new StreamReader(dataStream);
            //string responseFromServer = reader.ReadToEnd();
            //reader.Close();
            DataContractJsonSerializer jss = new DataContractJsonSerializer(typeof(CallLogData));
            callLogData = (CallLogData)jss.ReadObject(dataStream);
            dataStream.Close();
            response.Close();
        }
        private void getAttachments()
        {
            if (messageMetaData != null && messageMetaData.attachments != null && messageMetaData.attachments.Count > 0)
            {
                var url = RingCentral.Config.ApiUrl + "/restapi/v1.0/account/~/extension/~/message-store/" + MessageId + "/content/" + messageMetaData.attachments.First().id;
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
        private void getMetaData()
        {
            //var url = RingCentral.Config.ApiUrl + "/restapi/v1.0/account/~/extension/~/message-store/" + MessageId;
            //var url = "https://platform.devtest.ringcentral.com/restapi/v1.0/account/~/extension/~/message-store/ASoxYoFk3oTPYow"; // RingCentral.Config.ApiUrl + "/restapi/v1.0/account/~/extension/~/message-store/" + MessageId;
            var url = callLogData.records.First().recording.contentUri;
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.Headers.Add("Authorization", "Bearer " + AccessToken);
            request.Accept = "application/json";
            WebResponse response = request.GetResponse();
            Stream dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            DataContractJsonSerializer jss = new DataContractJsonSerializer(typeof(MessageMetaData));
            messageMetaData = (MessageMetaData)jss.ReadObject(dataStream);
            reader.Close();
            dataStream.Close();
            response.Close();
        }

		[DataContract]
		public class MessageMetaData
		{
			[DataMember(IsRequired = false)]
			public string id;
			[DataMember(IsRequired = false)]
			public string uri;
			[DataMember]
			public List<Attachment> attachments;
			public class Attachment
			{
				[DataMember]
				public string id;
				[DataMember(IsRequired = false)]
				public string uri;
				[DataMember(IsRequired = false)]
				public string type;
				[DataMember(IsRequired = false)]
				public string contentType;
				[DataMember(IsRequired = false)]
				public string vmDuration;
			}
		}

        [DataContract]
        public class CallLogData
        {
            [DataMember(IsRequired = false)]
            public string uri;
            [DataMember]
            public List<Record> records;
            [DataMember]
            public Navigation navigation;
            [DataMember]
            public Paging paging;
            public class Recording
            {
                [DataMember]
                public string id;
                [DataMember(IsRequired = false)]
                public string uri;
                [DataMember(IsRequired = false)]
                public string type;
                [DataMember(IsRequired = false)]
                public string contentUri;
            }
            public class From
            {
                [DataMember(IsRequired = false)]
                public string phoneNumber;
                [DataMember(IsRequired = false)]
                public string extensionNumber;
                [DataMember(IsRequired = false)]
                public string location;
                [DataMember(IsRequired = false)]
                public string name;
            }
            public class Navigation
            {
                [DataMember(IsRequired = false)]
                public Page firstPage;
                [DataMember(IsRequired = false)]
                public Page nextPage;
                [DataMember(IsRequired = false)]
                public Page previousPage;
                [DataMember(IsRequired = false)]
                public Page lastPage;
            }
            public class Paging
            {
                [DataMember(IsRequired = false)]
                public int page;
                [DataMember(IsRequired = false)]
                public int perPage;
                [DataMember(IsRequired = false)]
                public int pageStart;
                [DataMember(IsRequired = false)]
                public int pageEnd;
                [DataMember(IsRequired = false)]
                public int totalPages;
                [DataMember(IsRequired = false)]
                public int totalElements;
            }
            public class Page
            {
                [DataMember(IsRequired = false)]
                public string uri;
            }
            public class To
            {
                [DataMember(IsRequired = false)]
                public string phoneNumber;
                [DataMember(IsRequired = false)]
                public string extensionNumber;
                [DataMember(IsRequired = false)]
                public string location;
                [DataMember(IsRequired = false)]
                public string name;
            }
            public class Record
            {
                [DataMember]
                public string id;
                [DataMember(IsRequired = false)]
                public string uri;
                [DataMember(IsRequired = false)]
                public string sessionId;
                [DataMember(IsRequired = false)]
                public string type;
                [DataMember(IsRequired = false)]
                public string direction;
                [DataMember(IsRequired = false)]
                public string action;
                [DataMember(IsRequired = false)]
                public string result;
                [DataMember(IsRequired = false)]
                public string startTime;
                [DataMember(IsRequired = false)]
                public double duration;
                [DataMember(IsRequired = false)]
                public string lastModifiedTime;
                [DataMember(IsRequired = false)]
                public string transport;
                [DataMember(IsRequired = false)]
                public From from;
                [DataMember(IsRequired = false)]
                public To to;
                [DataMember(IsRequired = false)]
                public Recording recording;
            }
        }


	}
}
