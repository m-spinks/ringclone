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
    public class CreateWebhook : RingCentralAction 
    {
        public MessageData data;
        private string messageUrl;
        private string messageId;
        private List<string> eventFilters;
        public CreateWebhook(string ringCentralId) : base(ringCentralId)
        {
        }
        public override void DoAction()
        {
            try
            {
                var url = RingCentral.Config.ApiUrl + "/restapi/v1.0/subscription";
                var parms = new
                {
                    eventFilters = eventFilters,
                    deliveryMode = new
                    {
                        transportType = "WebHook",
                        address = "https://myapp.ngrok.io/hook?auth_token=MySecureToken"
                    }
                };
                var attrSerializer = new JavaScriptSerializer();
                var postString = attrSerializer.Serialize(parms);
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.Headers.Add("Authorization", "Bearer " + AccessToken);
                request.Accept = "application/json";
                request.ContentType = "application/json";
                StreamWriter requestWriter = new StreamWriter(request.GetRequestStream());
                requestWriter.Write(postString);
                requestWriter.Close();
                WebResponse response = request.GetResponse();
                Stream dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string responseFromServer = reader.ReadToEnd();
                reader.Close();
                //DataContractJsonSerializer jss = new DataContractJsonSerializer(typeof(MessageData));
                //data = (MessageData)jss.ReadObject(responseFromServer);
                dataStream.Close();
                response.Close();
            }
            catch (WebException ex)
            {
                var httpResponse = (HttpWebResponse)ex.Response;
                string err;
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    err = streamReader.ReadToEnd();
                }
                throw ex;
            }
        }

        public CreateWebhook AddEventFilter(string eventFilter)
        {
            if (eventFilters == null)
                eventFilters = new List<string>();
            if (!eventFilters.Any(x => x.ToLower() == eventFilter.ToLower()))
                eventFilters.Add(eventFilter);
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
