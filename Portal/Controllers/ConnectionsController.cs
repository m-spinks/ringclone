using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using RingClone.Portal.Models;
using NHibernate;
using RingClone.Portal.Helpers;
using NHibernate.Criterion;
using FluentNHibernate.Mapping;
using System.Web.Script.Serialization;
using RingClone.Portal.Filters;

namespace RingClone.Portal.Controllers
{
	public class ConnectionsController : Controller
	{
		[Authorize]
        [Register]
		public ActionResult Index()
		{
			var model = new ConnectionsModel();
			//getBoxAccountStat(model);
			getRingCentralStat(model);
			getCompleteDestinations(model);
			getIncompleteDestinations(model);
			getSimpleAutomationStatus(model);
			return View(model);
		}

		private void getRingCentralStat(ConnectionsModel model)
		{
            model.DisplayName = User.Identity.RingCloneIdentity().DisplayName;
            model.Company = User.Identity.RingCloneIdentity().Company;
        }

		private void getCompleteDestinations(ConnectionsModel model)
		{
			model.CompleteDestinations = new List<Models.ConnectionsModel.Destination>();
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

						var boxAccountCriteria = session.CreateCriteria<BoxAccountRec>();
						boxAccountCriteria.Add(Expression.Eq("AccountId", account.AccountId));
						boxAccountCriteria.Add(Expression.Eq("DeletedInd", false));
						var boxAccounts = boxAccountCriteria.List<BoxAccountRec>();
						if (boxAccounts.Any())
						{
							var boxAccount = boxAccounts.First();
							var dest = new ConnectionsModel.Destination()
							{
								DestinationId = boxAccount.BoxAccountId,
								DestinationTitle = "Box Account",
								DestinationType = "box"
							};
							model.CompleteDestinations.Add(dest);
						}

                        var googleAccountCriteria = session.CreateCriteria<GoogleAccountRec>();
                        googleAccountCriteria.Add(Expression.Eq("AccountId", account.AccountId));
                        googleAccountCriteria.Add(Expression.Eq("DeletedInd", false));
                        var googleAccounts = googleAccountCriteria.List<GoogleAccountRec>();
                        if (googleAccounts.Any())
                        {
                            var googleAccount = googleAccounts.First();
                            var dest = new ConnectionsModel.Destination()
                            {
                                DestinationId = googleAccount.GoogleAccountId,
                                DestinationTitle = "Google Acct",
                                DestinationType = "google"
                            };
                            model.CompleteDestinations.Add(dest);
                        }

                        var amazonAccountCriteria = session.CreateCriteria<AmazonAccountRec>();
                        amazonAccountCriteria.Add(Expression.Eq("AccountId", account.AccountId));
                        amazonAccountCriteria.Add(Expression.Eq("DeletedInd", false));
                        var amazonAccounts = amazonAccountCriteria.List<AmazonAccountRec>();
                        if (amazonAccounts.Any())
                        {
                            var amazonAccount = amazonAccounts.First();
                            var dest = new ConnectionsModel.Destination()
                            {
                                DestinationId = amazonAccount.AmazonAccountId,
                                DestinationTitle = "Amazon Acct",
                                DestinationType = "amazon"
                            };
                            model.CompleteDestinations.Add(dest);
                        }

                    }
                }
			}
		}

		private void getIncompleteDestinations(ConnectionsModel model)
		{
			model.IncompleteDestinations = new List<ConnectionsModel.Destination>();
			if (model.CompleteDestinations == null || !model.CompleteDestinations.Any(x => x.DestinationType == "box"))
			{
				var dest = new ConnectionsModel.Destination()
				{
					DestinationId = 0,
					DestinationTitle = "Box Account",
					DestinationType = "box"
				};
				model.IncompleteDestinations.Add(dest);
			}
            if (model.CompleteDestinations == null || !model.CompleteDestinations.Any(x => x.DestinationType == "google"))
            {
                var dest = new ConnectionsModel.Destination()
                {
                    DestinationId = 0,
                    DestinationTitle = "Google Account",
                    DestinationType = "google"
                };
                model.IncompleteDestinations.Add(dest);
            }
            if (model.CompleteDestinations == null || !model.CompleteDestinations.Any(x => x.DestinationType == "amazon"))
            {
                var dest = new ConnectionsModel.Destination()
                {
                    DestinationId = 0,
                    DestinationTitle = "Amazon Account",
                    DestinationType = "amazon"
                };
                model.IncompleteDestinations.Add(dest);
            }
        }

        private void getSimpleAutomationStatus(ConnectionsModel model)
		{
			model.SimpleAutomationExists = false;
			model.SimpleAutomationIsActive = false;
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
						var transferRuleCrit = session.CreateCriteria<TransferRuleRec>();
						transferRuleCrit.Add(Expression.Eq("AccountId", account.AccountId));
						var transferRules = transferRuleCrit.List<TransferRuleRec>();
						if (transferRules.Any())
						{
							model.SimpleAutomationExists = true;
							var transferRule = transferRules.First();
							if (!transferRule.DeletedInd)
							{
								model.SimpleAutomationIsActive = transferRule.ActiveInd;
								model.SimpleAutomationDestination = transferRule.Destination;
								model.SimpleAutomationFolder = transferRule.DestinationFolderLabel;
								model.SimpleAutomationPutInDatedSubfolder = transferRule.PutInDatedSubFolder;
							}
						}
					}
				}
			}
		}


		public class AccountRec
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
            public virtual string Email { get; set; }
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
                Map(x => x.Email);
                Map(x => x.AccessToken);
                Map(x => x.ExpiresIn);
                Map(x => x.TokenType);
                Map(x => x.RefreshToken);
                Map(x => x.DeletedInd);
                Map(x => x.LastRefreshedOn);
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
			public virtual string Email { get; set; }
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
				Map(x => x.Email);
				Map(x => x.AccessToken);
				Map(x => x.ExpiresIn);
				Map(x => x.TokenType);
				Map(x => x.RefreshToken);
				Map(x => x.DeletedInd);
				Map(x => x.LastRefreshedOn);
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
        private class TransferRuleRec
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
			public virtual bool DeletedInd { get; set; }
			public virtual bool ActiveInd { get; set; }
		}
		private class TransferRuleRecMap : ClassMap<TransferRuleRec>
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
				Map(x => x.DeletedInd);
				Map(x => x.ActiveInd);
			}
		}


	}
}
