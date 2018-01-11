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
    public class MessageStore : RingCentralAction 
    {
        public MessageStoreData data;
        public DateTime? dateTo;
        public DateTime? dateFrom;
        private string extension;
        private int page = 1;
        private int perPage = 100;
        private string navTo;
        private string messageType;
        public MessageStore(string ringCentralId) : base(ringCentralId)
        {
        }
        public override void DoAction()
        {
            if (string.IsNullOrEmpty(extension))
                extension = "~";
            //var rootUrl = RingCentral.Config.ApiUrl + "/restapi/v1.0/account/~/message-store";
            var rootUrl = RingCentral.Config.ApiUrl + "/restapi/v1.0/account/~/extension/" + extension + "/message-store";
            var requestParams = new NameValueCollection();
            if (dateFrom.HasValue)
                requestParams.Add("dateFrom", dateFrom.Value.ToString("yyyy-MM-ddTHH:mm:ssZ")); //ISO 8601 format including timezone, for example 2016-03-10T18:07:52.534Z.
            if (dateTo.HasValue)
                requestParams.Add("dateTo", endOfDay(dateTo.Value).ToString("yyyy-MM-ddTHH:mm:ssZ")); //ISO 8601 format including timezone, for example 2016-03-10T18:07:52.534Z.
            if (page != 0)
                requestParams.Add("page", page.ToString());
            if (perPage != 0)
                requestParams.Add("perPage", perPage.ToString());
            if (!string.IsNullOrWhiteSpace(messageType))
                requestParams.Add("messageType", messageType);
            var array = (from key in requestParams.AllKeys
                         from value in requestParams.GetValues(key)
                         select string.Format("{0}={1}", key, value))
                .ToArray();
            var queryString = string.Join("&", array);
            var url = rootUrl + "?" + queryString;
            if (!string.IsNullOrEmpty(navTo))
                url = navTo;
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.Headers.Add("Authorization", "Bearer " + AccessToken);
            request.Accept = "application/json";
            WebResponse response = request.GetResponse();
            Stream dataStream = response.GetResponseStream();
            //StreamReader reader = new StreamReader(dataStream);
            //string responseFromServer = reader.ReadToEnd();
            //reader.Close();
            DataContractJsonSerializer jss = new DataContractJsonSerializer(typeof(MessageStoreData));
            data = (MessageStoreData)jss.ReadObject(dataStream);
            dataStream.Close();
            response.Close();
        }

        public MessageStore DateFrom(DateTime dateFrom)
        {
            this.dateFrom = dateFrom;
            return this;
        }
        public MessageStore DateTo(DateTime dateTo)
        {
            this.dateTo = dateTo;
            return this;
        }
        public MessageStore Extension(string extension)
        {
            this.extension = extension;
            return this;
        }
        public MessageStore NavTo(string navTo)
        {
            this.navTo = navTo;
            return this;
        }
        public MessageStore Page(int page)
        {
            this.page = page;
            return this;
        }
        public MessageStore PerPage(int perPage)
        {
            this.perPage = perPage;
            return this;
        }
        private DateTime endOfDay(DateTime dateTime)
        {
            DateTime d = DateTime.Parse(dateTime.ToString("MM/dd/yyyy") + " 23:59:59");
            return d;
        }
        public MessageStore MessageType(string messageType)
        {
            this.messageType = messageType;
            return this;
        }

        [DataContract]
        public class MessageStoreData
        {
            [DataMember(IsRequired = false)]
            public string uri;
            [DataMember]
            public List<Record> records;
            [DataMember]
            public Navigation navigation;
            [DataMember]
            public Paging paging;
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
            }
        }

    }
}
