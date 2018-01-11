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
	public class AccountInfo : RingCentralAction
	{
		public AccountInfoData data;
		public class AccountInfoData
		{
			public string username { get; set; }
			public string id { get; set; }
			public string name { get; set; }
			public string type { get; set; }
			public string status { get; set; }
			public Contact contact { get; set; }
			public class Contact
			{
				public string firstName { get; set; }
				public string lastName { get; set; }
				public string company { get; set; }
				public string email { get; set; }
			}
		}

		public AccountInfo(string username)
			: base(username)
		{
			this.data = new AccountInfoData()
			{
				username = username,
				contact = new AccountInfoData.Contact()
			};
		}

		public override void DoAction()
		{
			var url = RingCentral.Config.ApiUrl + "/restapi/v1.0/account/~/extension/~";
			var request = (HttpWebRequest)WebRequest.Create(url);
			request.Method = "GET";
			request.Headers.Add("Authorization", "Bearer " + AccessToken);
			request.Accept = "application/json";
			WebResponse response = request.GetResponse();
			Stream dataStream = response.GetResponseStream();
			StreamReader reader = new StreamReader(dataStream);
			string responseFromServer = reader.ReadToEnd();
			JavaScriptSerializer jss = new JavaScriptSerializer();
			data = jss.Deserialize<AccountInfoData>(responseFromServer);
			reader.Close();
			dataStream.Close();
			response.Close();
		}

	}
}

