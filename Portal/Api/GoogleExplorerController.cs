using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using RingClone.Portal.Models;
using NHibernate;
using RingClone.Portal.Helpers;
using NHibernate.Criterion;
using FluentNHibernate.Mapping;
using System.IO;
using System.Web.Script.Serialization;
using System.Collections.Specialized;
using System.Web;

namespace RingClone.Portal.Api
{
    [Authorize]
    public class GoogleExplorerController : ApiController
    {
		[HttpGet]
		public GoogleExplorerModel Folder(int googleAccountId, string folderId, string folderName)
		{
			 var model = new GoogleExplorerModel();
			model.GoogleAccountId = googleAccountId;
			model.FolderId = folderId;
            model.FolderName = folderName;
			using (ISessionFactory sessionFactory = NHibernateHelper.CreateSessionFactory())
			{
				using (var session = sessionFactory.OpenSession())
				{
					var accountCrit = session.CreateCriteria<AccountRec>();
					accountCrit.Add(Expression.Eq("RingCentralId", User.Identity.RingCloneIdentity().RingCentralId));
					var accounts = accountCrit.List<AccountRec>();
					if (accounts.Any())
					{
						var account = accounts.First();
						var googleAccountCrit = session.CreateCriteria<GoogleAccountRec>();
						googleAccountCrit.Add(Expression.Eq("AccountId", account.AccountId));
						googleAccountCrit.Add(Expression.Eq("GoogleAccountId", model.GoogleAccountId));
						var googleAccounts = googleAccountCrit.List<GoogleAccountRec>();
						if (googleAccounts.Any())
						{
							var googleAccount = googleAccounts.First();
							getGoogleList(model);
						}
					}
				}
			}
			return model;
		}

		private void getGoogleList(GoogleExplorerModel model)
		{
			var f = new GoogleActions.GoogleFolder(User.Identity.RingCloneIdentity().RingCentralId, model.GoogleAccountId, model.FolderId);
            f.Execute();
			model.ChildFiles = new List<GoogleExplorerModel.GoogleFile>();
			model.ChildFolders = new List<GoogleExplorerModel.GoogleFolder>();
            if (string.IsNullOrEmpty(model.FolderId))
            {
                model.FolderId = f.FolderId;
            }
			//BUILD CHILD FOLDERS
			if (f.Folders != null && f.Folders.files != null)
			{
				foreach (var item in f.Folders.files)
				{
                    var newFolder = new GoogleExplorerModel.GoogleFolder()
                    {
                        FolderId = item.id,
                        FolderName = item.name
                    };
                    model.ChildFolders.Add(newFolder);
				}
			}
            if (f.Files != null && f.Files.files != null)
            {
                foreach (var item in f.Files.files)
                {
                    var newFile = new GoogleExplorerModel.GoogleFile()
                    {
                        FileId = item.id,
                        FileName = item.name
                    };
                    model.ChildFiles.Add(newFile);
                }
            }
            // BUILD BREADCRUMBS
            model.Breadcrumbs = new List<GoogleExplorerModel.Breadcrumb>();
            var breadcrumb = new GoogleExplorerModel.Breadcrumb();
            breadcrumb.FolderId = model.FolderId;
            breadcrumb.FolderName = model.FolderName;
            model.Breadcrumbs.Add(breadcrumb);
		}

		private class AccountRec
		{
			public virtual int AccountId { get; set; }
			public virtual string RingCentralId { get; set; }
		}
		private class AccountRecMap : ClassMap<AccountRec>
		{
			public AccountRecMap()
			{
				Table("T_ACCOUNT");
				Id(x => x.AccountId).Column("AccountId");
				Map(x => x.RingCentralId);
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
				Table("T_GOOGLEACCOUNT");
				Id(x => x.GoogleAccountId);
				Map(x => x.GoogleTokenId);
				Map(x => x.GoogleAccountName);
				Map(x => x.AccountId);
				Map(x => x.DeletedInd);
				Map(x => x.ActiveInd);
			}
		}

		public class GoogleTokenRec
		{
			public virtual int GoogleTokenId { get; set; }
			public virtual string AccessToken { get; set; }
			public virtual string ExpiresIn { get; set; }
			public virtual string TokenType { get; set; }
			public virtual string RefreshToken { get; set; }
			public virtual DateTime LastRefreshedOn { get; set; }
			public virtual bool DeletedInd { get; set; }
		}
		private class GoogleTokenRecMap : ClassMap<GoogleTokenRec>
		{
			public GoogleTokenRecMap()
			{
				Table("T_GOOGLETOKEN");
				Id(x => x.GoogleTokenId);
				Map(x => x.AccessToken);
				Map(x => x.ExpiresIn);
				Map(x => x.TokenType);
				Map(x => x.RefreshToken);
				Map(x => x.DeletedInd);
				Map(x => x.LastRefreshedOn);
			}
		}

		private class GoogleItem
		{
			public string id { get; set; }
			public string name { get; set; }
			public string description { get; set; }
			public PathCollection path_collection { get; set; }
			public ItemCollection item_collection { get; set; }
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
		}

	}

}
