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
    public class AmazonExplorerController : ApiController
    {
		[HttpGet]
		public AmazonExplorerModel Folder(int amazonAccountId, string path)
		{
            if (string.IsNullOrWhiteSpace(path))
                path = "";
            var model = new AmazonExplorerModel();
            model.Path = path.Trim(new char[] { '/' });
            model.AmazonAccountId = amazonAccountId;
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
						var amazonAccountCrit = session.CreateCriteria<AmazonAccountRec>();
						amazonAccountCrit.Add(Expression.Eq("AccountId", account.AccountId));
						amazonAccountCrit.Add(Expression.Eq("AmazonAccountId", model.AmazonAccountId));
						var amazonAccounts = amazonAccountCrit.List<AmazonAccountRec>();
						if (amazonAccounts.Any())
						{
							var amazonAccount = amazonAccounts.First();
							getAmazonList(model);
                            getUsability(model);
						}
					}
				}
			}
			return model;
		}

		private void getAmazonList(AmazonExplorerModel model)
		{
            //CONVERT GENERIC PATH TO OWNER, BUCKET, AND PREFIX
            //ALSO GET THE BREADCRUMBS
            var owner = "";
            var bucketName = "";
            var prefix = "";
            var breadCrumbPath = "";
            model.Breadcrumbs = new List<AmazonExplorerModel.Breadcrumb>();
            model.ChildFiles = new List<AmazonExplorerModel.AmazonFile>();
            model.ChildFolders = new List<AmazonExplorerModel.AmazonFolder>();
            if (!string.IsNullOrWhiteSpace(model.Path) && model.Path.Split(new char[] { '/' }).Count() > 1)
            {
                var pathItems = model.Path.Split(new char[] { '/' }).Where(x => !string.IsNullOrWhiteSpace(x));
                int iPath = 0;
                var type = "";
                foreach (var pathItem in pathItems)
                {
                    if (iPath == 0)
                    {
                        owner = pathItem;
                        breadCrumbPath = pathItem;
                        type = "owner";
                    }
                    if (iPath > 0)
                    {
                        breadCrumbPath += "/" + pathItem;
                    }
                    if (iPath == 1)
                    {
                        bucketName = pathItem;
                        type = "bucket";
                    }
                    if (iPath > 1)
                    {
                        type = "folder";
                    }
                    if (iPath == 2)
                    {
                        prefix += pathItem;
                    }
                    if (iPath > 2)
                    {
                        prefix += "/" + pathItem;
                    }
                    model.Breadcrumbs.Add(new AmazonExplorerModel.Breadcrumb()
                    {
                        FolderType = type,
                        FolderName = pathItem,
                        FolderId = pathItem,
                        FolderPath = breadCrumbPath
                    });
                    model.FolderName = pathItem;
                    iPath++;
                }
                if (!string.IsNullOrEmpty(prefix)) prefix += "/";
                var f = new AmazonActions.AmazonObjects(User.Identity.RingCloneIdentity().RingCentralId, model.AmazonAccountId, bucketName, prefix, "/");
                f.Execute();
                model.OwnerName = owner;
                model.BucketName = bucketName;
                model.Key = prefix;
                if (f.ResultException != null)
                    model.ErrorMessage = f.ResultException.Message;
                if (f.CommonPrefixes != null && f.CommonPrefixes.Any())
                {
                    foreach (var item in f.CommonPrefixes)
                    {
                        model.ChildFolders.Add(new AmazonExplorerModel.AmazonFolder()
                        {
                            FolderType = "folder",
                            FolderId = getFolderNameFromKey(item),
                            FolderName = getFolderNameFromKey(item),
                            FolderPath = breadCrumbPath + "/" + getFolderNameFromKey(item),
                            OwnerName = owner,
                            BucketName = bucketName,
                            Key = item
                        });
                    }
                }
            }
            else
            {
                var f = new AmazonActions.AmazonBuckets(User.Identity.RingCloneIdentity().RingCentralId, model.AmazonAccountId);
                f.Execute();
                model.Breadcrumbs.Add(new AmazonExplorerModel.Breadcrumb()
                {
                    FolderType = "owner",
                    FolderId = f.Owner,
                    FolderName = f.Owner,
                    FolderPath = f.Owner
                });
                if (f.Buckets != null && f.Buckets.Any())
                {
                    foreach (var item in f.Buckets)
                    {
                        model.ChildFolders.Add(new AmazonExplorerModel.AmazonFolder()
                        {
                            FolderType = "bucket",
                            FolderId = item.BucketName.Trim(new char[] { '/' }),
                            FolderName = item.BucketName.Trim(new char[] { '/' }),
                            FolderPath = f.Owner + "/" + item.BucketName.Trim(new char[] { '/' }),
                            BucketName = item.BucketName.Trim(new char[] { '/' }),
                            Key = "",
                            OwnerName = owner
                        });
                    }
                }
            }
		}
        private void getUsability(AmazonExplorerModel model)
        {
            if (model.Breadcrumbs == null || model.Breadcrumbs.Count < 2)
            {
                model.CanUseFolder = false;
            }
            else
            {
                model.CanUseFolder = true;
            }
            foreach (var folder in model.ChildFolders)
            {
                folder.CanUseFolder = true;
            }
        }

        private string getFolderNameFromKey(string key)
        {
            string folderName = "";
            var pathItems = key.Split(new char[] { '/' });
            foreach (var item in pathItems)
            {
                if (!string.IsNullOrEmpty(item))
                    folderName = item;
            }
            return folderName;
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

        public class AmazonAccountRec
        {
            public virtual int AmazonAccountId { get; set; }
            public virtual int AmazonUserId { get; set; }
            public virtual int AccountId { get; set; }
            public virtual string AmazonAccountName { get; set; }
            public virtual bool DeletedInd { get; set; }
            public virtual bool ActiveInd { get; set; }
        }
        private class AmazonAccountRecMap : ClassMap<AmazonAccountRec>
        {
            public AmazonAccountRecMap()
            {
                Table("T_AMAZONACCOUNT");
                Id(x => x.AmazonAccountId);
                Map(x => x.AmazonUserId);
                Map(x => x.AmazonAccountName);
                Map(x => x.AccountId);
                Map(x => x.DeletedInd);
                Map(x => x.ActiveInd);
            }
        }
        public class AmazonUserRec
        {
            public virtual int AmazonUserId { get; set; }
            public virtual string AccessKeyId { get; set; }
            public virtual string SecretAccessKey { get; set; }
            public virtual string DisplayName { get; set; }
            public virtual bool DeletedInd { get; set; }
        }
        private class AmazonUserRecMap : ClassMap<AmazonUserRec>
        {
            public AmazonUserRecMap()
            {
                Table("T_AMAZONUSER");
                Id(x => x.AmazonUserId);
                Map(x => x.AccessKeyId);
                Map(x => x.SecretAccessKey);
                Map(x => x.DisplayName);
                Map(x => x.DeletedInd);
            }
        }
	}
}
