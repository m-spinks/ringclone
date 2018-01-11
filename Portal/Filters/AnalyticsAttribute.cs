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
    public class AnalyticsAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (HttpContext.Current.User != null && HttpContext.Current.User.Identity != null && HttpContext.Current.User.Identity.RingCloneIdentity().RingCentralId != null)
            {
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
                            var loggedIn = false;
                            var choseFreePlanOnBillingPage = false;
                            var presentedWithUpgradeViaAutomationUrl = false;
                            var presentedWithUpgradeViaLetRingClone = false;
                            var selectedUpgradePlan = false;
                            var completedUpgradeBilling = false;
                            var completedBilling = false;
                            if (filterContext.ActionDescriptor.ControllerDescriptor.ControllerName.ToLower() == "account")
                            {
                                if (filterContext.ActionDescriptor.ActionName.ToLower() == "firsttimelogin")
                                    loggedIn = true;
                            }
                            if (filterContext.ActionDescriptor.ControllerDescriptor.ControllerName.ToLower() == "register")
                            {
                                if (filterContext.ActionDescriptor.ActionName.ToLower() == "chosefreeplanonbillingpage")
                                    choseFreePlanOnBillingPage = true;
                                else if (filterContext.ActionDescriptor.ActionName.ToLower() == "billing")
                                    loggedIn = true;
                                else if (filterContext.ActionDescriptor.ActionName.ToLower() == "completedbilling")
                                    completedBilling = true;
                            }
                            if (filterContext.ActionDescriptor.ControllerDescriptor.ControllerName.ToLower() == "simpleautomation")
                            {
                                if (filterContext.ActionDescriptor.ActionName.ToLower() == "viaautomationurl")
                                    presentedWithUpgradeViaAutomationUrl = true;
                                if (filterContext.ActionDescriptor.ActionName.ToLower() == "vialetringclone")
                                    presentedWithUpgradeViaLetRingClone = true;
                                else if (filterContext.ActionDescriptor.ActionName.ToLower() == "billing")
                                    selectedUpgradePlan = true;
                                else if (filterContext.ActionDescriptor.ActionName.ToLower() == "completedbilling")
                                    completedUpgradeBilling = true;
                            }
                            if (loggedIn || choseFreePlanOnBillingPage || presentedWithUpgradeViaAutomationUrl || presentedWithUpgradeViaLetRingClone || selectedUpgradePlan || completedUpgradeBilling || completedBilling)
                            {
                                AnalyticsRec aRec = null;
                                var analyticsCrit = session.CreateCriteria<AnalyticsRec>();
                                analyticsCrit.Add(Expression.Eq("AccountId", account.AccountId));
                                var analyticsList = analyticsCrit.List<AnalyticsRec>();
                                using (var transaction = session.BeginTransaction())
                                {
                                    if (analyticsList.Any())
                                        aRec = analyticsList.First();
                                    else
                                    {
                                        aRec = new AnalyticsRec();
                                        aRec.AccountId = account.AccountId;
                                        aRec.RefId = "";
                                        aRec.RefPlan = "";
                                        if (filterContext != null && filterContext.HttpContext != null && filterContext.HttpContext.Session != null && filterContext.HttpContext.Session["refid"] != null)
                                            aRec.RefId = filterContext.HttpContext.Session["refid"].ToString();
                                        if (filterContext != null && filterContext.HttpContext != null && filterContext.HttpContext.Session != null && filterContext.HttpContext.Session["refplan"] != null)
                                            aRec.RefPlan = filterContext.HttpContext.Session["refplan"].ToString();
                                    }
                                    if (loggedIn && !aRec.LoggedIn.HasValue)
                                        aRec.LoggedIn = DateTime.Now.ToUniversalTime();
                                    if (choseFreePlanOnBillingPage)
                                        aRec.ChoseFreePlanOnBillingPage = DateTime.Now.ToUniversalTime();
                                    if (presentedWithUpgradeViaAutomationUrl)
                                        aRec.PresentedWithUpgradeViaAutomationUrl = DateTime.Now.ToUniversalTime();
                                    if (presentedWithUpgradeViaLetRingClone)
                                        aRec.PresentedWithUpgradeViaLetRingClone = DateTime.Now.ToUniversalTime();
                                    if (selectedUpgradePlan)
                                        aRec.SelectedUpgradePlan = DateTime.Now.ToUniversalTime();
                                    if (completedUpgradeBilling)
                                        aRec.CompletedUpgradeBilling = DateTime.Now.ToUniversalTime();
                                    if (choseFreePlanOnBillingPage)
                                        aRec.ChoseFreePlanOnBillingPage = DateTime.Now.ToUniversalTime();
                                    if (completedBilling)
                                        aRec.CompletedBilling = DateTime.Now.ToUniversalTime();
                                    session.Save(aRec);
                                    transaction.Commit();
                                }
                            }
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
            }
        }
        private class AnalyticsRec
        {
            public virtual int AnalyticsId { get; set; }
            public virtual int AccountId { get; set; }
            public virtual string RefId { get; set; }
            public virtual string RefPlan { get; set; }
            public virtual DateTime? LoggedIn { get; set; }
            public virtual DateTime? ChoseFreePlanOnBillingPage { get; set; }
            public virtual DateTime? CompletedBilling { get; set; }
            public virtual DateTime? PresentedWithUpgradeViaAutomationUrl { get; set; }
            public virtual DateTime? PresentedWithUpgradeViaLetRingClone { get; set; }
            public virtual DateTime? SelectedUpgradePlan { get; set; }
            public virtual DateTime? CompletedUpgradeBilling { get; set; }
        }
        private class AnalyticsRecMap : ClassMap<AnalyticsRec>
        {
            public AnalyticsRecMap()
            {
                Table("T_ANALYTICS");
                Id(x => x.AnalyticsId).Column("AnalyticsId");
                Map(x => x.AccountId);
                Map(x => x.RefId);
                Map(x => x.RefPlan);
                Map(x => x.LoggedIn);
                Map(x => x.ChoseFreePlanOnBillingPage);
                Map(x => x.CompletedBilling);
                Map(x => x.PresentedWithUpgradeViaAutomationUrl);
                Map(x => x.PresentedWithUpgradeViaLetRingClone);
                Map(x => x.SelectedUpgradePlan);
                Map(x => x.CompletedUpgradeBilling);
            }
        }

    }

}