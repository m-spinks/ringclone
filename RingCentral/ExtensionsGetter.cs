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
    public class ExtensionsGetter : RingCentralAction 
    {
        public ExtensionResult data;
        private int perPage = 100;
        private int page = 1;
        private string navTo = "";
        public ExtensionsGetter(string ringCentralId)
            : base(ringCentralId)
        {
        }
        public override void DoAction()
        {
            var rootUrl = RingCentral.Config.ApiUrl + "/restapi/v1.0/account/~/extension";
            var requestParams = new NameValueCollection();
            requestParams.Add("perPage", perPage.ToString());
            if (page != 0)
                requestParams.Add("page", page.ToString());
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
            DataContractJsonSerializer jss = new DataContractJsonSerializer(typeof(ExtensionResult));
            data = (ExtensionResult)jss.ReadObject(dataStream);
            dataStream.Close();
            response.Close();
        }
        public ExtensionsGetter PerPage(int perPage)
        {
            this.perPage = perPage;
            return this;
        }
        public ExtensionsGetter NavTo(string navTo)
        {
            this.navTo = navTo;
            return this;
        }
        public ExtensionsGetter Page(int page)
        {
            this.page = page;
            return this;
        }


        [DataContract]
        public class ExtensionResult
        {
            [DataMember(IsRequired = false)]
            public IList<Record> records;
            [DataMember(IsRequired = false)]
            public Navigation navigation;

            public class Record
            {
                [DataMember(IsRequired = false)]
                public string id;
                [DataMember(IsRequired = false)]
                public string uri;
                [DataMember(IsRequired = false)]
                public string extensionNumber;
                [DataMember(IsRequired = false)]
                public string name;
                [DataMember(IsRequired = false)]
                public Contact contact;
            }
            public class Contact
            {
                [DataMember]
                public string firstName;
                [DataMember]
                public string lastName;
                [DataMember]
                public string email;
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

        }
    }
}
