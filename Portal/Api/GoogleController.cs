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
    public class GoogleController : ApiController
    {
		[HttpGet]
		public GoogleAccountInfoModel AccountInfo()
		{
            var model = new GoogleAccountInfoModel();
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
                        var crit = session.CreateCriteria<GoogleAccountRec>();
                        crit.Add(Expression.Eq("AccountId", account.AccountId));
                        crit.Add(Expression.Eq("DeletedInd", false));
                        var googleAccounts = crit.List<GoogleAccountRec>();
                        if (googleAccounts.Any())
                        {
                            var googleAccount = googleAccounts.First();
                            model.HasGoogleAccount = true;
                            model.GoogleAccountId = googleAccount.GoogleAccountId;
                            var googleEmail = new GoogleActions.GoogleEmail(User.Identity.RingCloneIdentity().RingCentralId, googleAccount.GoogleAccountId);
                            googleEmail.Execute();
                            if (!string.IsNullOrWhiteSpace(googleEmail.Email))
                            {
                                model.AbleToConnectToGoogleAccount = true;
                                if (googleEmail.Email == "matt.spinks@northtechnologies.com")
                                    model.GoogleAccountEmail = "my-email@northtechnologies.com";
                                else if (googleEmail.Email == "m.spinks@usa.com")
                                    model.GoogleAccountEmail = "my-email@mail.com";
                                else
                                    model.GoogleAccountEmail = googleEmail.Email;
                            }
                        }
                    }
				}
			}
			return model;
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
	}
}
