using FluentNHibernate.Mapping;
using NHibernate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using RingClone.Portal.Helpers;
using NHibernate.Criterion;

namespace RingClone.Portal.Filters
{
    public class NotCancelledAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (HttpContext.Current.User == null || HttpContext.Current.User.Identity == null || HttpContext.Current.User.Identity.RingCloneIdentity().RingCentralId == null)
            {
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary
                    {
                        { "action", "Login" },
                        { "controller", "Account" }
                    });
            }
            using (ISessionFactory sessionFactory = NHibernateHelper.CreateSessionFactory())
            {
                using (var session = sessionFactory.OpenSession())
                {
                    var accountCrit = session.CreateCriteria<AccountRec>();
                    accountCrit.Add(Expression.Eq("RingCentralId", HttpContext.Current.User.Identity.RingCloneIdentity().RingCentralId));
                    var accounts = accountCrit.List<AccountRec>();
                    if (accounts.Any())
                    {
                        var account = accounts.First();
                        if (account.CancelledInd)
                        {
                            filterContext.Result = new RedirectToRouteResult(
                                new RouteValueDictionary
                                {
                                    { "action", "Reenable" },
                                    { "controller", "Account" }
                                });
                        }
                    }
                }
            }
        }

        private class AccountRec
        {
            public virtual int AccountId { get; set; }
            public virtual string RingCentralId { get; set; }
            public virtual string StripeCustomerId { get; set; }
            public virtual bool PaymentIsCurrentInd { get; set; }
            public virtual bool RegisteredInd { get; set; }
			public virtual bool CancelledInd { get; set; }
		}
		private class AccountRecMap : ClassMap<AccountRec>
        {
            public AccountRecMap()
            {
                Table("T_ACCOUNT");
                Id(x => x.AccountId).Column("AccountId");
                Map(x => x.RingCentralId);
                Map(x => x.StripeCustomerId);
                Map(x => x.PaymentIsCurrentInd);
				Map(x => x.RegisteredInd);
				Map(x => x.CancelledInd);
			}
		}

    }

}