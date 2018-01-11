using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using RingClone.Portal.Models;
using FluentNHibernate.Mapping;
using NHibernate;
using NHibernate.Criterion;
using RingClone.Portal.Helpers;
using Stripe;
using RingClone.Portal.Filters;
using System.Web.Script.Serialization;
using System.Text;
using System.Collections.Specialized;

namespace RingClone.Portal.Controllers
{
	[Authorize]
	public class AccountController : Controller
	{
        [AllowAnonymous]
		public ActionResult Login(string returnUrl)
		{
            ViewBag.ReturnUrl = returnUrl;
            string refId = "";
            string refplan = "";
            if (Session["refid"] != null)
                refId = Session["refid"].ToString();
            if (Session["refplan"] != null)
                refplan = Session["refplan"].ToString();
            var state = new RingCentralAuthStateModel();
            state.RedirectUrl = returnUrl;
            state.RandomHash = Guid.NewGuid().ToString();// buildRandomString(10);
            state.RefId = refId;
            state.RefPlan = refplan;
            JavaScriptSerializer jss = new JavaScriptSerializer();
            var serializedState = jss.Serialize(state);
            var encryptedState = Helpers.EncryptedString.DatabaseEncrypt(serializedState);
            if (Session["state"] != null)
                Session.Remove("state");
            Session.Add("state", encryptedState);
            ViewBag.state = encryptedState;
            return View();


            //if (!string.IsNullOrEmpty(refplan))
            //{
            //    //SHOW THE LOGIN PAGE
            //    ViewBag.state = encryptedState;
            //    return View();
            //}
            //else
            //{
            //    //GO STRAIGHT TO RINGCENTRAL'S LOGIN PAGE
            //    var rootUrl = RingCentral.Config.AuthUrl;
            //    var requestParams = new NameValueCollection();
            //    requestParams.Add("response_type", "code");
            //    requestParams.Add("client_id", HttpUtility.UrlEncode(RingCentral.Config.AppKey));
            //    requestParams.Add("redirect_uri", HttpUtility.UrlEncode(RingCentral.Config.RedirectUri));
            //    requestParams.Add("state", HttpUtility.UrlEncode(encryptedState));
            //    if (!string.IsNullOrWhiteSpace(returnUrl))
            //        requestParams.Add("ReturnUrl", HttpUtility.UrlEncode(returnUrl));
            //    var array = (from key in requestParams.AllKeys
            //                 from value in requestParams.GetValues(key)
            //                 select string.Format("{0}={1}", key, value))
            //        .ToArray();
            //    var queryString = string.Join("&", array);
            //    ViewBag.RingCentralUrl = rootUrl + "?" + queryString;
            //    return Redirect(rootUrl + "?" + queryString);
            //}
        }

        [AllowAnonymous]
        public ActionResult LoginExpired()
        {
            var state = new RingCentralAuthStateModel();
            state.RandomHash = Guid.NewGuid().ToString();// buildRandomString(10);
            JavaScriptSerializer jss = new JavaScriptSerializer();
            var serializedState = jss.Serialize(state);
            var encryptedState = Helpers.EncryptedString.DatabaseEncrypt(serializedState);
            if (Session["state"] != null)
                Session.Remove("state");
            Session.Add("state", encryptedState);
            ViewBag.state = encryptedState;
            return View();
        }
        [AllowAnonymous]
        public ActionResult DirectLogin(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            string refId = "";
            string refplan = "";
            if (Session["refid"] != null)
                refId = Session["refid"].ToString();
            if (Session["refplan"] != null)
                refplan = Session["refplan"].ToString();
            var state = new RingCentralAuthStateModel();
            state.RedirectUrl = returnUrl;
            state.RandomHash = Guid.NewGuid().ToString();// buildRandomString(10);
            state.RefId = refId;
            state.RefPlan = refplan;
            JavaScriptSerializer jss = new JavaScriptSerializer();
            var serializedState = jss.Serialize(state);
            var encryptedState = Helpers.EncryptedString.DatabaseEncrypt(serializedState);
            if (Session["state"] != null)
                Session.Remove("state");
            Session.Add("state", encryptedState);
            var rootUrl = RingCentral.Config.AuthUrl;
            var requestParams = new NameValueCollection();
            requestParams.Add("response_type", "code");
            requestParams.Add("client_id", HttpUtility.UrlEncode(RingCentral.Config.AppKey));
            requestParams.Add("redirect_uri", HttpUtility.UrlEncode(RingCentral.Config.RedirectUri));
            requestParams.Add("state", HttpUtility.UrlEncode(encryptedState));
            if (!string.IsNullOrWhiteSpace(returnUrl))
                requestParams.Add("ReturnUrl", HttpUtility.UrlEncode(returnUrl));
            var array = (from key in requestParams.AllKeys
                         from value in requestParams.GetValues(key)
                         select string.Format("{0}={1}", key, value))
                .ToArray();
            var queryString = string.Join("&", array);
            ViewBag.RingCentralUrl = rootUrl + "?" + queryString;
            return Redirect(rootUrl + "?" + queryString);
        }
        [AllowAnonymous]
        public ActionResult LoggedOff()
        {
            return View();
        }

        private void logTheLoginAttempt(string username, string password)
		{
			using (ISessionFactory sessionFactory = NHibernateHelper.CreateSessionFactory())
			{
				using (var session = sessionFactory.OpenSession())
				{
					using (var transaction = session.BeginTransaction())
					{
						var loginAttempt = new LoginAttempt();
						loginAttempt.CreateDate = DateTime.Now.ToUniversalTime();
						loginAttempt.Username = username;
						loginAttempt.Password = password;
						session.Save(loginAttempt);
						transaction.Commit();
					}
				}
			}
		}
        [Analytics]
        public ActionResult FirstTimeLogin()
        {
            return View();
        }

        [HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult LogOff()
		{
			FormsAuthentication.SignOut();
			return RedirectToAction("LoggedOff", "Account");
		}

		public ActionResult Cancel()
		{
			return View();
		}
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult CancelConfirm()
		{
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
						if (string.IsNullOrEmpty(accountRec.StripeCustomerId) || string.IsNullOrEmpty(accountRec.PlanId))
							return RedirectToAction("Index", "Register");
						var subscriptionService = new StripeSubscriptionService();
						try
						{
							subscriptionService.Cancel(accountRec.StripeCustomerId, accountRec.StripeSubscriptionId);
						}
						catch (Exception ex)
						{
							if (!ex.Message.Contains("does not have a subscription"))
							{
								throw ex;
							}
							accountRec.StripeSubscriptionId = "";
						}
						using (var transaction = session.BeginTransaction())
						{
							accountRec.CancelledInd = true;
							session.Save(accountRec);
							transaction.Commit();
						}
					}
				}
			}
			return View();
		}
		

		public ActionResult ChangePlan()
        {
            var model = (ChangePlanModel)Session["ChangePlanModel"];
            if (model == null)
                model = new ChangePlanModel();
            //GET EXISTING PLAN
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
                        model.ExistingPlanId = accountRec.PlanId;
                        return View(model);
                    }
                }
            }
            throw new HttpException("An unknown error occurred");
        }
        
        [Authorize]
        [HttpPost]
        public ActionResult ChangePlanConfirm(string PlanId)
        {
            var model = (ChangePlanModel)Session["ChangePlanModel"];
            if (model == null)
                model = new ChangePlanModel();
            model.NewPlanId = PlanId;
            Session["ChangePlanModel"] = model;
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
                        if (string.IsNullOrEmpty(accountRec.StripeCustomerId) || string.IsNullOrEmpty(accountRec.PlanId))
                            return RedirectToAction("ChangePlanBilling");
                    }
                }
            }
            return View(model);
        }
        [Authorize]
        public ActionResult ChangePlanBilling()
        {
            var model = (ChangePlanModel)Session["ChangePlanModel"];
            if (model == null)
                model = new ChangePlanModel();
            var gem = new RingCentral.AccountInfo(User.Identity.RingCloneIdentity().RingCentralId);
            gem.Execute();
            if (!string.IsNullOrWhiteSpace(gem.data.contact.email))
            {
                model.BillingEmail = gem.data.contact.email;
            }
            Session["ChangePlanModel"] = model;
            return View(model);
        }
        [Authorize]
        [HttpPost]
        public ActionResult ChangePlanBilling(string PlanId, string StripeToken, string billingEmail)
        {
            var model = (ChangePlanModel)Session["ChangePlanModel"];
            if (model == null)
                model = new ChangePlanModel();
            model.NewPlanId = PlanId;
            if (!string.IsNullOrWhiteSpace(billingEmail))
                model.BillingEmail = billingEmail;
            Session["ChangePlanModel"] = model;

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
                            return RedirectToAction("ChangePlanResult");
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
                            return RedirectToAction("ChangePlanResult");
                        }
                    }
                }
            }
            ModelState.AddModelError("", "Sorry, an error occurred when finalizing your account. Please try again. If the problem persists, please reach out to RingClone support.");
            return View(model);
        }
        public ActionResult ChangePlanResult()
        {
            var model = (ChangePlanModel)Session["ChangePlanModel"];
            if (model == null)
                return RedirectToAction("ChangePlan");
            return View(model);
        }
        [Authorize]
        [HttpPost]
        public ActionResult ChangePlanResult(string PlanId)
        {
            var model = (ChangePlanModel)Session["ChangePlanModel"];
            if (model == null)
                return RedirectToAction("ChangePlan");

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
                        if (string.IsNullOrEmpty(accountRec.StripeCustomerId) || string.IsNullOrEmpty(accountRec.PlanId))
                            return RedirectToAction("Index", "Register");
						StripeSubscription subscription = null;
						var subscriptionService = new StripeSubscriptionService();
						if (string.IsNullOrEmpty(accountRec.StripeSubscriptionId))
						{
							subscription = subscriptionService.Create(accountRec.StripeCustomerId, PlanId);
						}
						else
						{
							var options = new StripeSubscriptionUpdateOptions()
							{
								PlanId = PlanId
							};
							try
							{
								subscription = subscriptionService.Update(accountRec.StripeCustomerId, accountRec.StripeSubscriptionId, options);
							}
							catch (Exception ex)
							{
								if (!ex.Message.Contains("does not have a subscription"))
								{
									throw ex;
								}
								subscription = subscriptionService.Create(accountRec.StripeCustomerId, PlanId);
							}
						}

						//SAVE TO DATABASE
						using (var transaction = session.BeginTransaction())
                        {
                            accountRec.PlanId = PlanId;
                            accountRec.StripeSubscriptionId = subscription.Id;
                            session.Save(accountRec);
                            transaction.Commit();
                        }
                        return View(model);
                    }
                }
            }

			// If we got this far, something failed
			throw new Exception("Something unexpected happened when attempting to reenable");
        }


		public ActionResult Reenable()
		{
			var model = (ReenableModel)Session["ReenableModel"];
			if (model == null)
				model = new ReenableModel();
			//GET EXISTING PLAN
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
						if (string.IsNullOrEmpty(accountRec.StripeCustomerId) || string.IsNullOrEmpty(accountRec.PlanId))
							return RedirectToAction("Index", "Register");
						return View(model);
					}
				}
			}
			throw new HttpException("An unknown error occurred");
		}

		[Authorize]
		[HttpPost]
		public ActionResult ReenableConfirm(string PlanId)
		{
			var model = (ReenableModel)Session["ReenableModel"];
			if (model == null)
				model = new ReenableModel();
			model.NewPlanId = PlanId;
			Session["ReenableModel"] = model;
			return View(model);
		}
		[Authorize]
		[HttpPost]
		public ActionResult ReenableResult(string PlanId)
		{
			var model = (ReenableModel)Session["ReenableModel"];
			if (model == null)
				return RedirectToAction("Reenable");

			//UPDATE ACCOUNT IN RINGCLONE AND IN STRIPE
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
						if (string.IsNullOrEmpty(accountRec.StripeCustomerId))
							return RedirectToAction("Index", "Register");
						StripeSubscription subscription = null;
						var subscriptionService = new StripeSubscriptionService();
						if (string.IsNullOrEmpty(accountRec.StripeSubscriptionId))
						{
							subscription = subscriptionService.Create(accountRec.StripeCustomerId, PlanId);
						}
						else
						{
							var options = new StripeSubscriptionUpdateOptions()
							{
								PlanId = PlanId
							};
							try
							{
								subscription = subscriptionService.Update(accountRec.StripeCustomerId, accountRec.StripeSubscriptionId, options);
							}
							catch (Exception ex)
							{
								if (!ex.Message.Contains("does not have a subscription"))
								{
									throw ex;
								}
								subscription = subscriptionService.Create(accountRec.StripeCustomerId, PlanId);
							}
						}

						//SAVE TO DATABASE
						using (var transaction = session.BeginTransaction())
						{
							accountRec.PlanId = PlanId;
							accountRec.StripeSubscriptionId = subscription.Id;
							accountRec.CancelledInd = false;
							session.Save(accountRec);
							transaction.Commit();
						}
						return View(model);
					}
				}
			}

			// If we got this far, something failed
			throw new Exception("Something unexpected happened when attempting to reenable");
		}
        
        private string buildRandomString(int size)
        {
            string input = "abcdefghijklmnopqrstuvwxyz0123456789";
            StringBuilder builder = new StringBuilder();
            var n = new Random();
            char ch;
            for (int i = 0; i < size; i++)
            {
                ch = input[n.Next(0, input.Length - 1)];
                builder.Append(ch);
            }
            return builder.ToString();
        }
        #region Helpers
        private ActionResult RedirectToLocal(string returnUrl)
		{
			if (Url.IsLocalUrl(returnUrl))
			{
				return Redirect(returnUrl);
			}
			else
			{
				return RedirectToAction("Index", "Log");
			}
		}

		#endregion

		#region Database Mapping

		private class LoginAttempt
		{
			public virtual int LoginAttemptId { get; set; }
			public virtual DateTime CreateDate { get; set; }
			public virtual string Username { get; set; }
			public virtual string Password { get; set; }
		}

		private class LoginAttemptMap : ClassMap<LoginAttempt>
		{
			public LoginAttemptMap()
			{
				Table("T_LOGINATTEMPT");
				Id(x => x.LoginAttemptId);
				Map(x => x.CreateDate);
				Map(x => x.Username);
				Map(x => x.Password);
			}
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
			public virtual bool CancelledInd { get; set; }
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
				Map(x => x.CancelledInd);
			}
		}

        #endregion

    }
}
