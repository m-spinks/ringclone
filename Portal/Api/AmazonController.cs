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
    public class AmazonController : ApiController
    {
		[HttpGet]
		public AmazonValidateModel Validate(string AccessKeyId, string SecretAccessKey, bool autoSave = false)
		{
            var model = new AmazonValidateModel();
            var vModel = AmazonActions.Helpers.Validate(AccessKeyId, SecretAccessKey);
            model.Validated = vModel.Validated;
            model.DisplayName = vModel.DisplayName;
            model.AccessKeyId = AccessKeyId;
            model.SecretAccessKey = SecretAccessKey;
            if (vModel.Validated && autoSave)
                saveAmazonUser(model);
            System.Threading.Thread.Sleep(2000);
            return model;
		}
        [HttpGet]
        public AmazonAccountInfoModel AccountInfo()
        {
            var model = new AmazonAccountInfoModel();
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
                        var crit = session.CreateCriteria<AmazonAccountRec>();
                        crit.Add(Expression.Eq("AccountId", account.AccountId));
                        crit.Add(Expression.Eq("DeletedInd", false));
                        var amazonAccounts = crit.List<AmazonAccountRec>();
                        if (amazonAccounts.Any())
                        {
                            var amazonAccount = amazonAccounts.First();
                            model.HasAmazonAccount = true;
                            model.AmazonAccountId = amazonAccount.AmazonAccountId;
                            var amazonAccountInfo = new AmazonActions.AmazonAccountInfo(User.Identity.RingCloneIdentity().RingCentralId, amazonAccount.AmazonAccountId);
                            amazonAccountInfo.Execute();
                            if (!string.IsNullOrWhiteSpace(amazonAccountInfo.DisplayName))
                            {
                                model.AbleToConnectToAmazonAccount = true;
                                model.DisplayName = amazonAccountInfo.DisplayName;
                            }
                        }
                    }
                }
            }

            return model;
        }

        public class AmazonValidateModel
        {
            public bool Validated;
            public string DisplayName;
            public string ErrorMessage;
            public string AccessKeyId;
            public string SecretAccessKey;
        }

        private void saveAmazonUser(AmazonValidateModel model)
        {
            using (ISessionFactory sessionFactory = NHibernateHelper.CreateSessionFactory())
            {
                using (var session = sessionFactory.OpenSession())
                {
                    var accountCrit = session.CreateCriteria<AccountRec>();
                    accountCrit.Add(Expression.Eq("RingCentralId", User.Identity.RingCloneIdentity().RingCentralId));
                    var accounts = accountCrit.List<AccountRec>();
                    if (accounts.Any())
                    {
                        using (var transaction = session.BeginTransaction())
                        {
                            AmazonAccountRec amazonAccount;
                            var account = accounts.First();
                            var amazonCrit = session.CreateCriteria<AmazonAccountRec>();
                            amazonCrit.Add(Expression.Eq("AccountId", account.AccountId));
                            var amazonAccounts = amazonCrit.List<AmazonAccountRec>();
                            if (amazonAccounts.Any())
                            {
                                amazonAccount = amazonAccounts.First();
                            }
                            else
                            {
                                amazonAccount = new AmazonAccountRec();
                                amazonAccount.AccountId = account.AccountId;
                                amazonAccount.ActiveInd = true;
                                amazonAccount.AmazonAccountName = "";
                                session.Save(amazonAccount);
                            }
                            if (amazonAccount.AmazonUserId == 0)
                            {
                                var amazonUser = new AmazonUserRec();
                                amazonUser.AccessKeyId = model.AccessKeyId;
                                amazonUser.SecretAccessKey = model.SecretAccessKey;
                                amazonUser.DisplayName = model.DisplayName;
                                session.Save(amazonUser);
                                amazonAccount.AmazonUserId = amazonUser.AmazonUserId;
                                session.Save(amazonAccount);
                            }
                            else
                            {
                                var amazonUserCrit = session.CreateCriteria<AmazonUserRec>();
                                amazonUserCrit.Add(Expression.Eq("AmazonUserId", amazonAccount.AmazonUserId));
                                var amazonUsers = amazonUserCrit.List<AmazonUserRec>();
                                if (amazonUsers.Any())
                                {
                                    var amazonUser = amazonUsers.First();
                                    amazonUser.AccessKeyId = model.AccessKeyId;
                                    amazonUser.SecretAccessKey = model.SecretAccessKey;
                                    amazonUser.DisplayName = model.DisplayName;
                                    session.Save(amazonUser);
                                }
                                else
                                {
                                    var amazonUser = new AmazonUserRec();
                                    amazonUser.AccessKeyId = model.AccessKeyId;
                                    amazonUser.SecretAccessKey = model.SecretAccessKey;
                                    amazonUser.DisplayName = model.DisplayName;
                                    session.Save(amazonUser);
                                    amazonAccount.AmazonUserId = amazonUser.AmazonUserId;
                                    session.Save(amazonAccount);
                                }
                            }
                            transaction.Commit();
                        }
                    }
                }
            }
        }

        #region Database Models

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

        #endregion
    }
}
