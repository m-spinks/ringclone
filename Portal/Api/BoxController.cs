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
    public class BoxController : ApiController
    {
		[HttpGet]
		public BoxAccountInfoModel AccountInfo()
		{
            var model = new BoxAccountInfoModel();
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
                        var crit = session.CreateCriteria<BoxAccountRec>();
                        crit.Add(Expression.Eq("AccountId", account.AccountId));
                        crit.Add(Expression.Eq("DeletedInd", false));
                        var boxAccounts = crit.List<BoxAccountRec>();
                        if (boxAccounts.Any())
                        {
                            var boxAccount = boxAccounts.First();
                            model.HasBoxAccount = true;
                            model.BoxAccountId = boxAccount.BoxAccountId;
                            var boxEmail = new Box.BoxEmail(User.Identity.RingCloneIdentity().RingCentralId, boxAccount.BoxAccountId);
                            boxEmail.Execute();
                            if (!string.IsNullOrWhiteSpace(boxEmail.Email))
                            {
                                model.AbleToConnectToBoxAccount = true;
                                if (boxEmail.Email == "matt.spinks@northtechnologies.com")
                                    model.BoxAccountEmail = "my-email@northtechnologies.com";
                                else if (boxEmail.Email == "m.spinks@usa.com")
                                    model.BoxAccountEmail = "my-email@mail.com";
                                else
                                    model.BoxAccountEmail = boxEmail.Email;
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
	}
}
