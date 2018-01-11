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
    public class CallLog : RingCentralAction 
    {
        public CallLogData data;
        public DateTime? dateTo;
        public DateTime? dateFrom;
        private bool? withRecording = true;
        private int page = 1;
        private int perPage = 100;
        private string extensionId;
        private string extensionNumber;
        private string type;
        private string navTo;
        public CallLog(string ringCentralId) : base(ringCentralId)
        {
        }
        public override void DoAction()
        {
            var rootUrl = "";
            if (!string.IsNullOrEmpty(extensionId))
                rootUrl = RingCentral.Config.ApiUrl + "/restapi/v1.0/account/~/extension/" + extensionId + "/call-log";
            else
                rootUrl = RingCentral.Config.ApiUrl + "/restapi/v1.0/account/~/call-log";
            var requestParams = new NameValueCollection();
            if (!string.IsNullOrWhiteSpace(type))
                requestParams.Add("type", type);
            if (!string.IsNullOrWhiteSpace(extensionNumber))
                requestParams.Add("extensionNumber", extensionNumber);
            if (withRecording.HasValue)
                requestParams.Add("withRecording", withRecording.Value.ToString());
            if (dateFrom.HasValue)
                requestParams.Add("dateFrom", dateFrom.Value.ToString("yyyy-MM-ddTHH:mm:ssZ")); //ISO 8601 format including timezone, for example 2016-03-10T18:07:52.534Z.
            if (dateTo.HasValue)
                requestParams.Add("dateTo", endOfDay(dateTo.Value).ToString("yyyy-MM-ddTHH:mm:ssZ")); //ISO 8601 format including timezone, for example 2016-03-10T18:07:52.534Z.
            if (page != 0)
                requestParams.Add("page", page.ToString());
            requestParams.Add("view", "Detailed");
            if (perPage != 0)
                requestParams.Add("perPage", perPage.ToString());
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
            DataContractJsonSerializer jss = new DataContractJsonSerializer(typeof(CallLogData));
            data = (CallLogData)jss.ReadObject(dataStream);
            dataStream.Close();
            response.Close();
        }

        public CallLog DateFrom(DateTime dateFrom)
        {
            this.dateFrom = dateFrom;
            return this;
        }
        public CallLog DateTo(DateTime dateTo)
        {
            this.dateTo = dateTo;
            return this;
        }
        public CallLog WithRecording(bool? withRecording)
        {
            this.withRecording = withRecording;
            return this;
        }
        public CallLog ExtensionId(string extensionId)
        {
            this.extensionId = extensionId;
            return this;
        }
        public CallLog ExtensionNumber(string extensionNumber)
        {
            this.extensionNumber = extensionNumber;
            return this;
        }
        public CallLog Type(string type)
        {
            this.type = type;
            return this;
        }
        public CallLog NavTo(string navTo)
        {
            this.navTo = navTo;
            return this;
        }
        public CallLog Page(int page)
        {
            this.page = page;
            return this;
        }
        public CallLog PerPage(int perPage)
        {
            this.perPage = perPage;
            return this;
        }
        public DateTime endOfDay(DateTime dateTime)
        {
            DateTime d = DateTime.Parse(dateTime.ToString("MM/dd/yyyy") + " 23:59:59");
            return d;
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
            public class RecordingInfo
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
                public DeviceInfo device;
            }
            public class DeviceInfo
            {
                [DataMember]
                public string id;
                [DataMember(IsRequired = false)]
                public string uri;
            }
            public class ExtensionInfo
            {
                [DataMember]
                public string id;
                [DataMember(IsRequired = false)]
                public string uri;
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
            public class Billing
            {
                [DataMember(IsRequired = false)]
                public string costIncluded;
                [DataMember(IsRequired = false)]
                public string costPurchased;
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
                public int duration;
                [DataMember(IsRequired = false)]
                public string lastModifiedTime;
                [DataMember(IsRequired = false)]
                public string transport;
                [DataMember(IsRequired = false)]
                public CallerInfo from;
                [DataMember(IsRequired = false)]
                public CallerInfo to;
                [DataMember(IsRequired = false)]
                public RecordingInfo recording;
                [DataMember(IsRequired = false)]
                public VoicemailMessageInfo message;
                [DataMember(IsRequired = false)]
                public Billing billing;
                [DataMember(IsRequired = false)]
                public List<Leg> legs;
            }
            public class Leg
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
                public int duration;
                [DataMember(IsRequired = false)]
                public string lastModifiedTime;
                [DataMember(IsRequired = false)]
                public string transport;
                [DataMember(IsRequired = false)]
                public string legType;
                [DataMember(IsRequired = false)]
                public ExtensionInfo extension;
                [DataMember(IsRequired = false)]
                public CallerInfo from;
                [DataMember(IsRequired = false)]
                public CallerInfo to;
                [DataMember(IsRequired = false)]
                public RecordingInfo recording;
                [DataMember(IsRequired = false)]
                public VoicemailMessageInfo message;
                [DataMember(IsRequired = false)]
                public Billing billing;
            }
            public class VoicemailMessageInfo
            {
                [DataMember]
                public string uri;
                [DataMember]
                public string id;
                [DataMember]
                public string type;

            }
        }

    }
}
