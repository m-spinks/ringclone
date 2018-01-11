using FluentNHibernate.Mapping;
using NHibernate;
using NHibernate.Criterion;
using RingClone.Portal.Helpers;
using RingClone.Portal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;

namespace RingClone.Portal.Api
{
    [Authorize]
    public class SimpleAutomationController : ApiController
    {
        [HttpGet]
        public string Index()
        {
			using (ISessionFactory sessionFactory = NHibernateHelper.CreateSessionFactory())
			{
				using (var session = sessionFactory.OpenSession())
				{
					var accountCrit = session.CreateCriteria<AccountRecord>();
					accountCrit.Add(Expression.Eq("RingCentralId", User.Identity.RingCloneIdentity().RingCentralId));
					var accounts = accountCrit.List<AccountRecord>();
					if (accounts.Any())
					{
						var account = accounts.First();
						var transferRuleCrit = session.CreateCriteria<TransferRuleRecord>();
						transferRuleCrit.Add(Expression.Eq("AccountId", account.AccountId));
						var transferRules = transferRuleCrit.List<TransferRuleRecord>();
						if (transferRules.Any())
						{
							var transferRule = transferRules.First();
							if (transferRule.ActiveInd && !transferRule.DeletedInd)
							{
								return "active";
							}
							else
							{
								return "not active";
							}
						}
					}
				}
			}
			return "";
		}
		[HttpPost]
		public void On()
		{
			using (ISessionFactory sessionFactory = NHibernateHelper.CreateSessionFactory())
			{
				using (var session = sessionFactory.OpenSession())
				{
					TransferRuleRecord transferRule = null;
					var accountCrit = session.CreateCriteria<AccountRecord>();
					accountCrit.Add(Expression.Eq("RingCentralId", User.Identity.RingCloneIdentity().RingCentralId));
					var accounts = accountCrit.List<AccountRecord>();
					if (accounts.Any())
					{
						var account = accounts.First();
						var transferRuleCrit = session.CreateCriteria<TransferRuleRecord>();
						transferRuleCrit.Add(Expression.Eq("AccountId", account.AccountId));
						var transferRules = transferRuleCrit.List<TransferRuleRecord>();
						if (transferRules.Any())
						{
							transferRule = transferRules.First();
							using (var transaction = session.BeginTransaction())
							{
								transferRule.ActiveInd = true;
								transferRule.DeletedInd = false;
								session.Save(transferRule);
								transaction.Commit();
							}
						}
					}
				}
			}
		}
		[HttpPost]
		public void Off()
		{
			using (ISessionFactory sessionFactory = NHibernateHelper.CreateSessionFactory())
			{
				using (var session = sessionFactory.OpenSession())
				{
					TransferRuleRecord transferRule = null;
					var accountCrit = session.CreateCriteria<AccountRecord>();
					accountCrit.Add(Expression.Eq("RingCentralId", User.Identity.RingCloneIdentity().RingCentralId));
					var accounts = accountCrit.List<AccountRecord>();
					if (accounts.Any())
					{
						var account = accounts.First();
						var transferRuleCrit = session.CreateCriteria<TransferRuleRecord>();
						transferRuleCrit.Add(Expression.Eq("AccountId", account.AccountId));
						var transferRules = transferRuleCrit.List<TransferRuleRecord>();
						if (transferRules.Any())
						{
							transferRule = transferRules.First();
							using (var transaction = session.BeginTransaction())
							{
								transferRule.ActiveInd = false;
								session.Save(transferRule);
								transaction.Commit();
							}
						}
					}
				}
			}
		}
        [HttpPost]
        public void PutInDatedSubfolder(string id)
        {
            using (ISessionFactory sessionFactory = NHibernateHelper.CreateSessionFactory())
            {
                using (var session = sessionFactory.OpenSession())
                {
                    TransferRuleRecord transferRule = null;
                    var accountCrit = session.CreateCriteria<AccountRecord>();
                    accountCrit.Add(Expression.Eq("RingCentralId", User.Identity.RingCloneIdentity().RingCentralId));
                    var accounts = accountCrit.List<AccountRecord>();
                    if (accounts.Any())
                    {
                        var account = accounts.First();
                        var transferRuleCrit = session.CreateCriteria<TransferRuleRecord>();
                        transferRuleCrit.Add(Expression.Eq("AccountId", account.AccountId));
                        var transferRules = transferRuleCrit.List<TransferRuleRecord>();
                        if (transferRules.Any())
                        {
                            transferRule = transferRules.First();
                            using (var transaction = session.BeginTransaction())
                            {
                                transferRule.PutInDatedSubFolder = (id == "on");
                                session.Save(transferRule);
                                transaction.Commit();
                            }
                        }
                    }
                }
            }
        }
        [HttpPost]
        public void VoiceLog(string id)
        {
            using (ISessionFactory sessionFactory = NHibernateHelper.CreateSessionFactory())
            {
                using (var session = sessionFactory.OpenSession())
                {
                    TransferRuleRecord transferRule = null;
                    var accountCrit = session.CreateCriteria<AccountRecord>();
                    accountCrit.Add(Expression.Eq("RingCentralId", User.Identity.RingCloneIdentity().RingCentralId));
                    var accounts = accountCrit.List<AccountRecord>();
                    if (accounts.Any())
                    {
                        var account = accounts.First();
                        var transferRuleCrit = session.CreateCriteria<TransferRuleRecord>();
                        transferRuleCrit.Add(Expression.Eq("AccountId", account.AccountId));
                        var transferRules = transferRuleCrit.List<TransferRuleRecord>();
                        if (transferRules.Any())
                        {
                            transferRule = transferRules.First();
                            using (var transaction = session.BeginTransaction())
                            {
                                transferRule.VoiceLogInd = (id == "on");
                                session.Save(transferRule);
                                transaction.Commit();
                            }
                        }
                    }
                }
            }
        }
        [HttpPost]
        public void VoiceContent(string id)
        {
            using (ISessionFactory sessionFactory = NHibernateHelper.CreateSessionFactory())
            {
                using (var session = sessionFactory.OpenSession())
                {
                    TransferRuleRecord transferRule = null;
                    var accountCrit = session.CreateCriteria<AccountRecord>();
                    accountCrit.Add(Expression.Eq("RingCentralId", User.Identity.RingCloneIdentity().RingCentralId));
                    var accounts = accountCrit.List<AccountRecord>();
                    if (accounts.Any())
                    {
                        var account = accounts.First();
                        var transferRuleCrit = session.CreateCriteria<TransferRuleRecord>();
                        transferRuleCrit.Add(Expression.Eq("AccountId", account.AccountId));
                        var transferRules = transferRuleCrit.List<TransferRuleRecord>();
                        if (transferRules.Any())
                        {
                            transferRule = transferRules.First();
                            using (var transaction = session.BeginTransaction())
                            {
                                transferRule.VoiceContentInd = (id == "on");
                                session.Save(transferRule);
                                transaction.Commit();
                            }
                        }
                    }
                }
            }
        }
        [HttpPost]
        public void FaxLog(string id)
        {
            using (ISessionFactory sessionFactory = NHibernateHelper.CreateSessionFactory())
            {
                using (var session = sessionFactory.OpenSession())
                {
                    TransferRuleRecord transferRule = null;
                    var accountCrit = session.CreateCriteria<AccountRecord>();
                    accountCrit.Add(Expression.Eq("RingCentralId", User.Identity.RingCloneIdentity().RingCentralId));
                    var accounts = accountCrit.List<AccountRecord>();
                    if (accounts.Any())
                    {
                        var account = accounts.First();
                        var transferRuleCrit = session.CreateCriteria<TransferRuleRecord>();
                        transferRuleCrit.Add(Expression.Eq("AccountId", account.AccountId));
                        var transferRules = transferRuleCrit.List<TransferRuleRecord>();
                        if (transferRules.Any())
                        {
                            transferRule = transferRules.First();
                            using (var transaction = session.BeginTransaction())
                            {
                                transferRule.FaxLogInd = (id == "on");
                                session.Save(transferRule);
                                transaction.Commit();
                            }
                        }
                    }
                }
            }
        }
        [HttpPost]
        public void FaxContent(string id)
        {
            using (ISessionFactory sessionFactory = NHibernateHelper.CreateSessionFactory())
            {
                using (var session = sessionFactory.OpenSession())
                {
                    TransferRuleRecord transferRule = null;
                    var accountCrit = session.CreateCriteria<AccountRecord>();
                    accountCrit.Add(Expression.Eq("RingCentralId", User.Identity.RingCloneIdentity().RingCentralId));
                    var accounts = accountCrit.List<AccountRecord>();
                    if (accounts.Any())
                    {
                        var account = accounts.First();
                        var transferRuleCrit = session.CreateCriteria<TransferRuleRecord>();
                        transferRuleCrit.Add(Expression.Eq("AccountId", account.AccountId));
                        var transferRules = transferRuleCrit.List<TransferRuleRecord>();
                        if (transferRules.Any())
                        {
                            transferRule = transferRules.First();
                            using (var transaction = session.BeginTransaction())
                            {
                                transferRule.FaxContentInd = (id == "on");
                                session.Save(transferRule);
                                transaction.Commit();
                            }
                        }
                    }
                }
            }
        }
        [HttpPost]
        public void SmsLog(string id)
        {
            using (ISessionFactory sessionFactory = NHibernateHelper.CreateSessionFactory())
            {
                using (var session = sessionFactory.OpenSession())
                {
                    TransferRuleRecord transferRule = null;
                    var accountCrit = session.CreateCriteria<AccountRecord>();
                    accountCrit.Add(Expression.Eq("RingCentralId", User.Identity.RingCloneIdentity().RingCentralId));
                    var accounts = accountCrit.List<AccountRecord>();
                    if (accounts.Any())
                    {
                        var account = accounts.First();
                        var transferRuleCrit = session.CreateCriteria<TransferRuleRecord>();
                        transferRuleCrit.Add(Expression.Eq("AccountId", account.AccountId));
                        var transferRules = transferRuleCrit.List<TransferRuleRecord>();
                        if (transferRules.Any())
                        {
                            transferRule = transferRules.First();
                            using (var transaction = session.BeginTransaction())
                            {
                                transferRule.SmsLogInd = (id == "on");
                                session.Save(transferRule);
                                transaction.Commit();
                            }
                        }
                    }
                }
            }
        }
        [HttpPost]
        public void SmsContent(string id)
        {
            using (ISessionFactory sessionFactory = NHibernateHelper.CreateSessionFactory())
            {
                using (var session = sessionFactory.OpenSession())
                {
                    TransferRuleRecord transferRule = null;
                    var accountCrit = session.CreateCriteria<AccountRecord>();
                    accountCrit.Add(Expression.Eq("RingCentralId", User.Identity.RingCloneIdentity().RingCentralId));
                    var accounts = accountCrit.List<AccountRecord>();
                    if (accounts.Any())
                    {
                        var account = accounts.First();
                        var transferRuleCrit = session.CreateCriteria<TransferRuleRecord>();
                        transferRuleCrit.Add(Expression.Eq("AccountId", account.AccountId));
                        var transferRules = transferRuleCrit.List<TransferRuleRecord>();
                        if (transferRules.Any())
                        {
                            transferRule = transferRules.First();
                            using (var transaction = session.BeginTransaction())
                            {
                                transferRule.SmsContentInd = (id == "on");
                                session.Save(transferRule);
                                transaction.Commit();
                            }
                        }
                    }
                }
            }
        }

        #region Database
        private class AccountRecord
		{
			public virtual int AccountId { get; set; }
			public virtual string RingCentralId { get; set; }
		}
		private class AccountRecMap : ClassMap<AccountRecord>
		{
			public AccountRecMap()
			{
				Table("T_ACCOUNT");
				Id(x => x.AccountId).Column("AccountId");
				Map(x => x.RingCentralId);
			}
		}

		private class TransferRuleRecord
		{
			public virtual int TransferRuleId { get; set; }
			public virtual int AccountId { get; set; }
			public virtual string Destination { get; set; }
			public virtual int DestinationBoxAccountId { get; set; }
			public virtual int DestinationGoogleAccountId { get; set; }
			public virtual int DestinationFtpAccountId { get; set; }
			public virtual string DestinationFolderId { get; set; }
			public virtual string DestinationFolderPath { get; set; }
			public virtual string DestinationFolderName { get; set; }
			public virtual string DestinationFolderLabel { get; set; }
			public virtual bool PutInDatedSubFolder { get; set; }
			public virtual string Frequency { get; set; }
			public virtual string DayOf { get; set; }
			public virtual string TimeOfDay { get; set; }
            public virtual bool VoiceLogInd { get; set; }
            public virtual bool VoiceContentInd { get; set; }
            public virtual bool FaxLogInd { get; set; }
            public virtual bool FaxContentInd { get; set; }
            public virtual bool SmsLogInd { get; set; }
            public virtual bool SmsContentInd { get; set; }
            public virtual bool DeletedInd { get; set; }
			public virtual bool ActiveInd { get; set; }
		}
		private class TransferRuleRecMap : ClassMap<TransferRuleRecord>
		{
			public TransferRuleRecMap()
			{
				Table("T_TRANSFERRULE");
				Id(x => x.TransferRuleId).Column("TransferRuleId");
				Map(x => x.AccountId);
				Map(x => x.Destination);
				Map(x => x.DestinationBoxAccountId);
				Map(x => x.DestinationGoogleAccountId);
				Map(x => x.DestinationFtpAccountId);
				Map(x => x.DestinationFolderId);
				Map(x => x.DestinationFolderPath);
				Map(x => x.DestinationFolderName);
				Map(x => x.DestinationFolderLabel);
				Map(x => x.PutInDatedSubFolder);
				Map(x => x.Frequency);
				Map(x => x.DayOf);
				Map(x => x.TimeOfDay);
                Map(x => x.VoiceLogInd);
                Map(x => x.VoiceContentInd);
                Map(x => x.FaxLogInd);
                Map(x => x.FaxContentInd);
                Map(x => x.SmsLogInd);
                Map(x => x.SmsContentInd);
                Map(x => x.DeletedInd);
				Map(x => x.ActiveInd);
			}
		}
		#endregion

	}


}