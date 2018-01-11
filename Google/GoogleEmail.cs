using FluentNHibernate.Mapping;
using NHibernate;
using NHibernate.Criterion;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;

namespace GoogleActions
{
    public class GoogleEmail : GoogleAction
    {
        public string Email;
        public GoogleEmail(string username, int googleAccountId)
            : base(username, googleAccountId)
        {
            
        }
        public override void DoAction()
        {
			var googleEmailUrl = "https://www.googleapis.com/userinfo/email";
			var requestParams = new NameValueCollection();
			requestParams.Add("alt", "json");
			requestParams.Add("access_token", AccessToken);
			var array = (from key in requestParams.AllKeys
				 from value in requestParams.GetValues(key)
				 select string.Format("{0}={1}", HttpUtility.HtmlEncode(key), HttpUtility.HtmlEncode(value)))
				.ToArray();
			var queryString = string.Join("&", array);
			var url = googleEmailUrl + "?" + queryString;
			WebRequest request = WebRequest.Create(url);
			request.Method = "GET";
			request.ContentType = "application/x-www-form-urlencoded";
			WebResponse response = request.GetResponse();
			Stream dataStream = response.GetResponseStream();
			StreamReader reader = new StreamReader(dataStream);
			string responseFromServer = reader.ReadToEnd();
			JavaScriptSerializer jss = new JavaScriptSerializer();
			var responseModel = jss.Deserialize<GoogleEmailResponse>(responseFromServer);
            Email = responseModel.data.email;
			reader.Close();
			dataStream.Close();
			response.Close();
        }

        public class AccountRec
        {
            public virtual int AccountId { get; set; }
            public virtual string RingCentralId { get; set; }
            public virtual string RingCentralExtension { get; set; }
        }
        private class AccountRecMap : ClassMap<AccountRec>
        {
            public AccountRecMap()
            {
                Table("T_ACCOUNT");
                Id(x => x.AccountId).Column("AccountId");
                Map(x => x.RingCentralId);
                Map(x => x.RingCentralExtension);
            }
        }
        public class GoogleAccountRec
        {
            public virtual int GoogleAccountId { get; set; }
            public virtual int GoogleTokenId { get; set; }
            public virtual int AccountId { get; set; }
            public virtual string GoogleAccountName { get; set; }
            public virtual bool DeletedInd { get; set; }
            public virtual bool ActiveInd { get; set; }
        }
        private class GoogleAccountRecMap : ClassMap<GoogleAccountRec>
        {
            public GoogleAccountRecMap()
            {
                Table("T_BOXACCOUNT");
                Id(x => x.GoogleAccountId);
                Map(x => x.GoogleTokenId);
                Map(x => x.GoogleAccountName);
                Map(x => x.AccountId);
                Map(x => x.DeletedInd);
                Map(x => x.ActiveInd);
            }
        }

        private class GoogleEmailResponse
        {
            public Data data;
            public class Data
            {
                public string email { get; set; }
            }
        }
    }
}
