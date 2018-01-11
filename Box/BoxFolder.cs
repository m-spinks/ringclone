using Box.Models;
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
using System.Web.Script.Serialization;

namespace Box
{
    public class BoxFolder : BoxAction
    {
		public BoxFolderData data { get; set; }
		private string access_token { get; set; }
		private string refresh_token { get; set; }
		private string folder_id { get; set; }
		private int box_account_id { get; set; }
		public class BoxFolderData
		{
			public string id { get; set; }
			public string name { get; set; }
			public string description { get; set; }
			public PathCollection path_collection { get; set; }
			public ItemCollection item_collection { get; set; }
		}

		public class PathCollection
		{
			public int total_Count { get; set; }
			public List<Entry> entries { get; set; }
		}
		public class ItemCollection
		{
			public int total_Count { get; set; }
			public List<Entry> entries { get; set; }
		}
		public class Entry
		{
			public string type { get; set; }
			public string id { get; set; }
			public string sequence_id { get; set; }
			public string name { get; set; }
		}
		public BoxFolder(string username, int boxAccountId, string folderId) : base(username, boxAccountId)
		{
			this.box_account_id = boxAccountId;
			this.folder_id = folderId;
		}

        public override void DoAction()
        {
            var boxEmailUrl = "https://api.box.com/2.0/folders/" + folder_id;
            var requestParams = new NameValueCollection();
            requestParams.Add("access_token", AccessToken);
            var array = (from key in requestParams.AllKeys
                         from value in requestParams.GetValues(key)
                         select string.Format("{0}={1}", key, value))
                .ToArray();
            var queryString = string.Join("&", array);
            var url = boxEmailUrl + "?" + queryString;
            WebRequest request = WebRequest.Create(boxEmailUrl);
            request.Method = "GET";
            request.ContentType = "application/x-www-form-urlencoded";
            request.Headers.Add("Authorization", "Bearer " + AccessToken);
            WebResponse response = request.GetResponse();
            Stream dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string responseFromServer = reader.ReadToEnd();
            JavaScriptSerializer jss = new JavaScriptSerializer();
            data = jss.Deserialize<BoxFolderData>(responseFromServer);
            reader.Close();
            dataStream.Close();
            response.Close();
        }

		public class BoxAccountRec
		{
			public virtual int BoxAccountId { get; set; }
			public virtual int BoxTokenId { get; set; }
			public virtual int AccountId { get; set; }
			public virtual string BoxAccountName { get; set; }
			public virtual bool DeletedInd { get; set; }
			public virtual bool ActiveInd { get; set; }
		}
		private class BoxAccountRecMap : ClassMap<BoxAccountRec>
		{
			public BoxAccountRecMap()
			{
				Table("T_BOXACCOUNT");
				Id(x => x.BoxAccountId);
				Map(x => x.BoxTokenId);
				Map(x => x.BoxAccountName);
				Map(x => x.AccountId);
				Map(x => x.DeletedInd);
				Map(x => x.ActiveInd);
			}
		}

		public class BoxTokenRec
		{
			public virtual int BoxTokenId { get; set; }
			public virtual string AccessToken { get; set; }
			public virtual string ExpiresIn { get; set; }
			public virtual string TokenType { get; set; }
			public virtual string RefreshToken { get; set; }
			public virtual DateTime LastRefreshedOn { get; set; }
			public virtual bool DeletedInd { get; set; }
		}
		private class BoxTokenRecMap : ClassMap<BoxTokenRec>
		{
			public BoxTokenRecMap()
			{
				Table("T_BOXTOKEN");
				Id(x => x.BoxTokenId);
				Map(x => x.AccessToken);
				Map(x => x.ExpiresIn);
				Map(x => x.TokenType);
				Map(x => x.RefreshToken);
				Map(x => x.DeletedInd);
				Map(x => x.LastRefreshedOn);
			}
		}
    }
}
