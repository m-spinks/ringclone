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
    public class BoxExplorerController : ApiController
    {
		[HttpGet]
		public BoxExplorerModel Folder(int boxAccountId, string folderId)
		{
			var model = new BoxExplorerModel();
			model.BoxAccountId = boxAccountId;
			model.FolderId = folderId;
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
						var boxAccountCrit = session.CreateCriteria<BoxAccountRec>();
						boxAccountCrit.Add(Expression.Eq("AccountId", account.AccountId));
						boxAccountCrit.Add(Expression.Eq("BoxAccountId", model.BoxAccountId));
						var boxAccounts = boxAccountCrit.List<BoxAccountRec>();
						if (boxAccounts.Any())
						{
							var boxAccount = boxAccounts.First();
							getBoxList(model);
						}
					}
				}
			}
			return model;
		}

		private void getBoxList(BoxExplorerModel model)
		{
			var f = new Box.BoxFolder(User.Identity.RingCloneIdentity().RingCentralId, model.BoxAccountId, model.FolderId);
            f.Execute();
			model.ChildFiles = new List<BoxExplorerModel.BoxFile>();
			model.ChildFolders = new List<BoxExplorerModel.BoxFolder>();
			//BUILD CHILD FOLDERS
			if (f.data.item_collection != null)
			{
				foreach (var item in f.data.item_collection.entries)
				{
					if (item.type == "folder")
					{
						var newFolder = new BoxExplorerModel.BoxFolder()
						{
							FolderId = item.id,
							FolderName = item.name
						};
						model.ChildFolders.Add(newFolder);
					}
					else if (item.type == "file")
					{
						var newFile = new BoxExplorerModel.BoxFile()
						{
							FileId = item.id,
							FileName = item.name
						};
						model.ChildFiles.Add(newFile);
					}
				}
			}
			// BUILD BREADCRUMBS
			model.Breadcrumbs = new List<BoxExplorerModel.Breadcrumb>();
			if (f.data.path_collection != null)
			{
				foreach (var item in f.data.path_collection.entries)
				{
					var breadcrumb = new BoxExplorerModel.Breadcrumb();
					breadcrumb.FolderId = item.id;
					breadcrumb.FolderName = item.name;
					model.Breadcrumbs.Add(breadcrumb);
				}
			}
			var lastBreadcrumb = new BoxExplorerModel.Breadcrumb();
			lastBreadcrumb.FolderId = f.data.id;
			lastBreadcrumb.FolderName = f.data.name;
			model.Breadcrumbs.Add(lastBreadcrumb);
			model.FolderId = lastBreadcrumb.FolderId;
			model.FolderName = lastBreadcrumb.FolderName;
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

		private class BoxItem
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
