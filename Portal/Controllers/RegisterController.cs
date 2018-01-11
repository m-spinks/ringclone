using FluentNHibernate.Mapping;
using NHibernate;
using NHibernate.Criterion;
using RingClone.Portal.Filters;
using RingClone.Portal.Helpers;
using RingClone.Portal.Models;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace RingClone.Portal.Controllers
{
    public class RegisterController : Controller
    {
        [Authorize]
        [Analytics]
        public ActionResult Index(string plan = "")
        {
            var planId = "ringclone_bronze";
            if (!string.IsNullOrWhiteSpace(plan))
            {
                if (plan.ToLower().Contains("silver"))
                    planId = "ringclone_silver";
                if (plan.ToLower().Contains("gold"))
                    planId = "ringclone_gold";
                if (plan.ToLower().Contains("platinum"))
                    planId = "ringclone_platinum";
            }
            else if (Session["refplan"] != null && !string.IsNullOrWhiteSpace(Session["refplan"].ToString()))
            {
                if (Session["refplan"].ToString().ToLower().Contains("silver"))
                    planId = "ringclone_silver";
                if (Session["refplan"].ToString().ToLower().Contains("gold"))
                    planId = "ringclone_gold";
                if (Session["refplan"].ToString().ToLower().Contains("platinum"))
                    planId = "ringclone_platinum";
            }
            if (planId == "ringclone_bronze")
            {
                using (ISessionFactory sessionFactory = NHibernateHelper.CreateSessionFactory())
                {
                    using (var session = sessionFactory.OpenSession())
                    {
                        AccountRec accountRec;
                        var accountCrit = session.CreateCriteria<AccountRec>();
                        accountCrit.Add(Expression.Eq("RingCentralId", User.Identity.RingCloneIdentity().RingCentralId));
                        var accounts = accountCrit.List<AccountRec>();
                        if (accounts.Any())
                        {
                            accountRec = accounts.First();
                            using (var transaction = session.BeginTransaction())
                            {
                                accountRec.StripeCustomerId = "";
                                accountRec.StripeSubscriptionId = "";
                                accountRec.PlanId = planId;
                                accountRec.RegisteredInd = true;
                                accountRec.PaymentIsCurrentInd = true;
                                session.Save(accountRec);
                                transaction.Commit();
                            }
                        }
                    }
                }
                return RedirectToAction("Index", "Log");
            }
            else
			{
                var model = (RegisterModel)Session["RegisterModel"];
                if (model == null)
                    model = new RegisterModel();
                model.PlanId = planId;
                Session["RegisterModel"] = model;
                return RedirectToAction("Billing");
            }
        }
        public ActionResult AlreadyLoggedIn()
		{
			return View();
		}
        [Analytics]
        public ActionResult Billing()
		{
            var model = (RegisterModel)Session["RegisterModel"];
            if (model == null)
                return RedirectToAction("Index");

            var gem = new RingCentral.AccountInfo(User.Identity.RingCloneIdentity().RingCentralId);
            gem.Execute();
            if (!string.IsNullOrWhiteSpace(gem.data.contact.email))
            {
                model.BillingEmail = gem.data.contact.email;
                return View(model);
            }
            return RedirectToAction("Index");
        }
        [HttpPost]
		public ActionResult Billing(string PlanId, string StripeToken, string billingEmail)
		{
            var model = (RegisterModel)Session["RegisterModel"];
            if (model == null)
                return RedirectToAction("Index");
            if (!string.IsNullOrWhiteSpace(billingEmail))
                model.BillingEmail = billingEmail;

            //UPDATE ACCOUNT IN RINGCLONE
            using (ISessionFactory sessionFactory = NHibernateHelper.CreateSessionFactory())
            {
                using (var session = sessionFactory.OpenSession())
                {
                    AccountRec accountRec;
                    var accountCrit = session.CreateCriteria<AccountRec>();
                    accountCrit.Add(Expression.Eq("RingCentralId", User.Identity.RingCloneIdentity().RingCentralId));
                    var accounts = accountCrit.List<AccountRec>();
                    if (accounts.Any())
                    {
                        accountRec = accounts.First();
                        if (!string.IsNullOrEmpty(accountRec.StripeCustomerId))
                        {
                            var stripeCustomerOptions = new StripeCustomerUpdateOptions();
                            stripeCustomerOptions.Email = model.BillingEmail;
                            stripeCustomerOptions.Description = "";
                            stripeCustomerOptions.SourceToken = StripeToken;
                            var customerService = new StripeCustomerService();
                            StripeCustomer stripeCustomer = customerService.Update(accountRec.StripeCustomerId, stripeCustomerOptions);
                            using (var transaction = session.BeginTransaction())
                            {
                                accountRec.RegisteredInd = true;
                                accountRec.PaymentIsCurrentInd = true;
                                accountRec.PlanId = PlanId;
                                session.Save(accountRec);
                                transaction.Commit();
                            }
                            return RedirectToAction("CompletedBilling");
                        }
                        else
                        {
                            var stripeCustomerOptions = new StripeCustomerCreateOptions();
                            stripeCustomerOptions.Email = model.BillingEmail;
                            stripeCustomerOptions.Description = "";
                            stripeCustomerOptions.SourceToken = StripeToken;
                            stripeCustomerOptions.PlanId = PlanId;
							//stripeCustomerOptions.TaxPercent = 8.16m;
							stripeCustomerOptions.TrialEnd = DateTime.UtcNow.AddDays(30);
                            stripeCustomerOptions.Quantity = 1;
                            //CREATE CUSTOMER IN STRIPE
                            var customerService = new StripeCustomerService();
                            StripeCustomer stripeCustomer = customerService.Create(stripeCustomerOptions);
                            //ASSOCIATE STRIPE CUSTOMER WITH PLAN
                            var subscriptionService = new StripeSubscriptionService();
                            StripeSubscription stripeSubscription = subscriptionService.Create(stripeCustomer.Id, PlanId); // optional StripeSubscriptionCreateOptions
                            //SAVE TO DATABASE
                            using (var transaction = session.BeginTransaction())
                            {
                                accountRec.StripeCustomerId = stripeCustomer.Id;
                                accountRec.StripeSubscriptionId = stripeSubscription.Id;
                                accountRec.PlanId = PlanId;
                                accountRec.RegisteredInd = true;
                                accountRec.PaymentIsCurrentInd = true;
                                session.Save(accountRec);
                                transaction.Commit();
                            }
                            return RedirectToAction("CompletedBilling");
                        }
                    }
                }
            }

			// If we got this far, something failed, redisplay form
			ModelState.AddModelError("", "Sorry, an error occurred when finalizing your account. Please try again. If the problem persists, please reach out to RingClone support.");
			return View(model);
		}
        [Analytics]
        public ActionResult CompletedBilling()
        {
            return View();
        }
        [Analytics]
        public ActionResult ChoseFreePlanOnBillingPage()
        {
            using (ISessionFactory sessionFactory = NHibernateHelper.CreateSessionFactory())
            {
                using (var session = sessionFactory.OpenSession())
                {
                    AccountRec accountRec;
                    var accountCrit = session.CreateCriteria<AccountRec>();
                    accountCrit.Add(Expression.Eq("RingCentralId", User.Identity.RingCloneIdentity().RingCentralId));
                    var accounts = accountCrit.List<AccountRec>();
                    if (accounts.Any())
                    {
                        accountRec = accounts.First();
                        using (var transaction = session.BeginTransaction())
                        {
                            accountRec.StripeCustomerId = "";
                            accountRec.StripeSubscriptionId = "";
                            accountRec.PlanId = "ringclone_bronze";
                            accountRec.RegisteredInd = true;
                            accountRec.PaymentIsCurrentInd = true;
                            session.Save(accountRec);
                            transaction.Commit();
                        }
                    }
                }
            }
            return RedirectToAction("Index", "Log");
        }


        public class AccountRec
        {
            public virtual string AccountId { get; set; }
            public virtual string RingCentralId { get; set; }
            public virtual string RingCentralExtension { get; set; }
            public virtual int RingCentralTokenId { get; set; }
            public virtual string StripeCustomerId { get; set; }
            public virtual string StripeSubscriptionId { get; set; }
            public virtual string PlanId { get; set; }
            public virtual bool PaymentIsCurrentInd { get; set; }
            public virtual bool RegisteredInd { get; set; }
        }

        private class AccountRecMap : ClassMap<AccountRec>
        {
            public AccountRecMap()
            {
                Table("T_ACCOUNT");
                Id(x => x.AccountId).Column("AccountId");
                Map(x => x.StripeCustomerId);
                Map(x => x.StripeSubscriptionId);
                Map(x => x.PlanId);
                Map(x => x.RingCentralTokenId);
                Map(x => x.RingCentralId);
                Map(x => x.RingCentralExtension);
                Map(x => x.PaymentIsCurrentInd);
                Map(x => x.RegisteredInd);
            }
        }

        public class RingCentralTokenRec
        {
            public virtual int RingCentralTokenId { get; set; }
            public virtual string AccessToken { get; set; }
            public virtual string ExpiresIn { get; set; }
            public virtual string TokenType { get; set; }
            public virtual string RefreshToken { get; set; }
            public virtual string RefreshTokenExpiresIn { get; set; }
            public virtual string Scope { get; set; }
            public virtual string OwnerId { get; set; }
            public virtual string EndpointId { get; set; }
            public virtual DateTime LastRefreshedOn { get; set; }
            public virtual bool DeletedInd { get; set; }
        }
        private class RingCentralTokenRecMap : ClassMap<RingCentralTokenRec>
        {
            public RingCentralTokenRecMap()
            {
                Table("T_RINGCENTRALTOKEN");
                Id(x => x.RingCentralTokenId);
                Map(x => x.AccessToken);
                Map(x => x.ExpiresIn);
                Map(x => x.TokenType);
                Map(x => x.RefreshToken);
                Map(x => x.RefreshTokenExpiresIn);
                Map(x => x.DeletedInd);
                Map(x => x.LastRefreshedOn);
                Map(x => x.Scope);
                Map(x => x.OwnerId);
                Map(x => x.EndpointId);
            }
        }


    }
}
