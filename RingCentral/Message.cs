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
    public class Message : RingCentralAction 
    {
        public MessageData data;
        private string messageUrl;
        private string messageId;
        public Message(string ringCentralId) : base(ringCentralId)
        {
        }
        public override void DoAction()
        {
            var url = RingCentral.Config.ApiUrl + "/restapi/v1.0/account/~/extension/~/message-store";
            var requestParams = new NameValueCollection();
            if (!string.IsNullOrEmpty(messageId))
                url += "/" + messageId;
            if (!string.IsNullOrEmpty(messageUrl))
                url = messageUrl;
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.Headers.Add("Authorization", "Bearer " + AccessToken);
            request.Accept = "application/json";
            WebResponse response = request.GetResponse();
            Stream dataStream = response.GetResponseStream();
            //StreamReader reader = new StreamReader(dataStream);
            //string responseFromServer = reader.ReadToEnd();
            //reader.Close();
            DataContractJsonSerializer jss = new DataContractJsonSerializer(typeof(MessageData));
            data = (MessageData)jss.ReadObject(dataStream);
            dataStream.Close();
            response.Close();
        }

        public Message MessageUrl(string messageUrl)
        {
            this.messageUrl = messageUrl;
            return this;
        }
        public Message MessageId(string messageId)
        {
            this.messageId = messageId;
            return this;
        }

        [DataContract]
        public class MessageData
        {
            [DataMember]
            public string id;
            [DataMember(IsRequired = false)]
            public string uri;

            [DataMember]
            public List<MessageAttachmentInfo> attachments;

            [DataMember(IsRequired = false)]
            public string availability;
            [DataMember(IsRequired = false)]
            public string conversationId;
            [DataMember(IsRequired = false)]
            public ConversationInfo conversation;
            [DataMember(IsRequired = false)]
            public string creationTime;
            [DataMember(IsRequired = false)]
            public string deliveryErrorCode;
            [DataMember(IsRequired = false)]
            public string direction;
            [DataMember(IsRequired = false)]
            public int faxPageCount;
            [DataMember(IsRequired = false)]
            public string faxResolution;
            [DataMember(IsRequired = false)]
            public int coverIndex;
            [DataMember(IsRequired = false)]
            public string coverPageText;

            [DataMember(IsRequired = false)]
            public CallerInfo from;

            [DataMember(IsRequired = false)]
            public string lastModifiedTime;
            [DataMember(IsRequired = false)]
            public string messageStatus;
            [DataMember(IsRequired = false)]
            public bool pgToDepartment;
            [DataMember(IsRequired = false)]
            public string priority;
            [DataMember(IsRequired = false)]
            public string readStatus;

            [DataMember(IsRequired = false)]
            public string smsDeliveryTime;
            [DataMember(IsRequired = false)]
            public string smsSendingAttemptsCount;

            [DataMember(IsRequired = false)]
            public string subject;

            [DataMember(IsRequired = false)]
            public List<CallerInfo> to;

            [DataMember(IsRequired = false)]
            public string type;
            [DataMember(IsRequired = false)]
            public string vmTranscriptionStatus;
            public class CallerInfo
            {
                [DataMember(IsRequired = false)]
                public string phoneNumber;
                [DataMember(IsRequired = false)]
                public string extensionNumber;
                [DataMember(IsRequired = false)]
                public string location;
                [DataMember(IsRequired = false)]
                public string name;
                [DataMember(IsRequired = false)]
                public string target;
                [DataMember(IsRequired = false)]
                public string messageStatus;
                [DataMember(IsRequired = false)]
                public string faxErrorCode;
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
            public class MessageAttachmentInfo
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
                public double vmDuration;
            }
            public class ConversationInfo
            {
                [DataMember]
                public string id;
                [DataMember(IsRequired = false)]
                public string uri;
            }
            public class Record
            {
            }
        }

    }
}
