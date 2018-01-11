using FluentNHibernate.Mapping;
using NHibernate;
using NHibernate.Criterion;
using RingClone.Portal.Filters;
using RingClone.Portal.Helpers;
using RingClone.Portal.Models;
using Stripe;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace RingClone.Portal.Controllers
{
    [Register]
	[NotCancelled]
	[Authorize]
	public class SimpleAutomationController : Controller
    {
        public ActionResult Index(string returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl))
                Session["SimpleAutomationReturnUrl"] = returnUrl;
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
                        if (accountRec.PlanId.ToLower().Contains("bronze"))
                        {
                            return RedirectToAction("ViaAutomationUrl");
                        }
                    }
                }
            }
            var model = getModelFromDatabase();
            if (model != null)
            {
                Session["SimpleAutomationModel"] = model;
                //CONVERT UTC TO CST FOR DISPLAY
                var h = 0;
                var m = 0;
                if (!string.IsNullOrWhiteSpace(model.TimeOfDay) && model.TimeOfDay.Length >= 4 && int.TryParse(model.TimeOfDay.Substring(0, 2), out h) && int.TryParse(model.TimeOfDay.Substring(2, 2), out m))
                {
                    TimeZoneInfo cst = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
                    var now = DateTime.Now.ToUniversalTime();
                    DateTime utcTime = new DateTime(now.Year, now.Month, now.Day, h, m, 00, DateTimeKind.Utc);
                    model.TimeOfDay = TimeZoneInfo.ConvertTimeFromUtc(utcTime, cst).ToString("hh:mm tt");
                }
                return View(model);
            }
            return RedirectToAction("SelectDestination"); ;
        }
        [Analytics]
        public ActionResult ViaLetRingClone(string returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl))
                Session["SimpleAutomationReturnUrl"] = returnUrl;
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
                        if (accountRec.PlanId.ToLower().Contains("bronze"))
                        {
                            return RedirectToAction("UpgradePlan");
                        }
                    }
                }
            }
            return RedirectToAction("Index");
        }
        [Analytics]
        public ActionResult ViaAutomationUrl(string returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl))
                Session["SimpleAutomationReturnUrl"] = returnUrl;
            return RedirectToAction("UpgradePlan");
        }
        public ActionResult UpgradePlan()
        {
            var model = (SimpleAutomationModel)Session["SimpleAutomationModel"];
            if (model == null)
                model = new SimpleAutomationModel();
            Session["SimpleAutomationModel"] = model;
            return View(model);
        }
		[HttpPost]
		public ActionResult UpgradePlan(string PlanId)
		{
			var model = (SimpleAutomationModel)Session["SimpleAutomationModel"];
			if (model == null)
				return RedirectToAction("Index");

			model.PlanId = PlanId;
			return RedirectToAction("Billing");
		}
        [Analytics]
        public ActionResult Billing()
		{
			var model = (SimpleAutomationModel)Session["SimpleAutomationModel"];
			if (model == null)
				model = new SimpleAutomationModel();
			Session["SimpleAutomationModel"] = model;
			var gem = new RingCentral.AccountInfo(User.Identity.RingCloneIdentity().RingCentralId);
			gem.Execute();
			if (!string.IsNullOrWhiteSpace(gem.data.contact.email))
			{
				model.BillingEmail = gem.data.contact.email;
			}
            return View(model);
        }
        [HttpPost]
		public ActionResult Billing(string PlanId, string StripeToken, string billingEmail)
		{
			var model = (SimpleAutomationModel)Session["SimpleAutomationModel"];
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
								session.Save(accountRec);
								transaction.Commit();
							}
							return RedirectToAction("CompletedBilling");
						}
					}
				}
			}

			// If we got this far, something failed, redisplay form
			ModelState.AddModelError("", "Sorry, an error occurred when changing. Please try again. If the problem persists, please reach out to RingClone support.");
			return View(model);
		}
        [Analytics]
        public ActionResult CompletedBilling()
        {
            return View();
        }
        public ActionResult SelectDestination(int transferRuleId = 0)
        {
            SimpleAutomationModel model = null;
            if (transferRuleId > 0)
            {
                model = getModelFromDatabase();
                if (model != null)
                {
                    Session["SimpleAutomationModel"] = model;
                    return View(model);
                }
            }
            model = (SimpleAutomationModel)Session["SimpleAutomationModel"];
            if (model == null)
                model = new SimpleAutomationModel();
            Session["SimpleAutomationModel"] = model;
            return View(model);
        }
        public ActionResult UseBox(int transferRuleId = 0)
        {
            SimpleAutomationModel model = null;
            if (transferRuleId > 0)
            {
                model = getModelFromDatabase();
                if (model != null)
                    Session["SimpleAutomationModel"] = model;
            }
            else
            {
                model = (SimpleAutomationModel)Session["SimpleAutomationModel"];
            }
            if (model == null)
                return RedirectToAction("Index");
            model.Destination = "box";

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
                        var boxAccountRec = session.CreateCriteria<BoxAccountRec>();
                        boxAccountRec.Add(Expression.Eq("AccountId", account.AccountId));
                        var boxAccounts = boxAccountRec.List<BoxAccountRec>();
                        if (boxAccounts.Any())
                        {
                            var boxAccount = boxAccounts.First();
                            var authorizeChecker = new Box.AuthChecker(User.Identity.RingCloneIdentity().RingCentralId, boxAccount.BoxAccountId);
                            authorizeChecker.Execute();
                            if (authorizeChecker.IsAuthenticated)
                            {
                                var boxTokenCrit = session.CreateCriteria<BoxTokenRec>();
                                boxTokenCrit.Add(Expression.Eq("BoxTokenId", boxAccount.BoxTokenId));
                                var boxTokens = boxTokenCrit.List<BoxTokenRec>();
                                if (boxTokens.Any())
                                {
                                    var boxToken = boxTokens.First();
                                    model.BoxEmail = boxToken.Email;
                                    return RedirectToAction("ChooseBoxFolder");
                                }
                            }
                        }
                    }
                }
            }

            JavaScriptSerializer jss = new JavaScriptSerializer();
            var serializedState = jss.Serialize(createBoxStateModel(model));
            var encryptedState = Helpers.EncryptedString.DatabaseEncrypt(serializedState);
            var url = AppConfig.Box.AuthUri;
            var requestParams = new NameValueCollection();
            requestParams.Add("response_type", "code");
            requestParams.Add("client_id", AppConfig.Box.ClientId);
            requestParams.Add("redirect_uri", AppConfig.Box.RedirectUri);
            requestParams.Add("state", encryptedState);
            var array = (from key in requestParams.AllKeys
                         from value in requestParams.GetValues(key)
                         select string.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(value)))
                .ToArray();
            var queryString = string.Join("&", array);
            model.BoxUrl = url + "?" + queryString;
            Session.Add("state", encryptedState);
            return View(model);

        }
        public ActionResult UseGoogle(int transferRuleId = 0)
        {
            SimpleAutomationModel model = null;
            if (transferRuleId > 0)
            {
                model = getModelFromDatabase();
                if (model != null)
                    Session["SimpleAutomationModel"] = model;
            }
            else
            {
                model = (SimpleAutomationModel)Session["SimpleAutomationModel"];
            }
            if (model == null)
                return RedirectToAction("Index");
            model.Destination = "google";

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
                        var googleAccountRec = session.CreateCriteria<GoogleAccountRec>();
                        googleAccountRec.Add(Expression.Eq("AccountId", account.AccountId));
                        var googleAccounts = googleAccountRec.List<GoogleAccountRec>();
                        if (googleAccounts.Any())
                        {
                            var googleAccount = googleAccounts.First();
                            var authorizeChecker = new GoogleActions.AuthChecker(User.Identity.RingCloneIdentity().RingCentralId, googleAccount.GoogleAccountId);
                            authorizeChecker.Execute();
                            if (authorizeChecker.IsAuthenticated)
                            {
                                var googleTokenCrit = session.CreateCriteria<GoogleTokenRec>();
                                googleTokenCrit.Add(Expression.Eq("GoogleTokenId", googleAccount.GoogleTokenId));
                                var googleTokens = googleTokenCrit.List<GoogleTokenRec>();
                                if (googleTokens.Any())
                                {
                                    var googleToken = googleTokens.First();
                                    model.GoogleEmail = googleToken.Email;
                                    return RedirectToAction("ChooseGoogleFolder");
                                }
                            }
                        }
                    }
                }
            }

			JavaScriptSerializer jss = new JavaScriptSerializer();
			var serializedState = jss.Serialize(createGoogleStateModel(model));
			var encryptedState = Helpers.EncryptedString.DatabaseEncrypt(serializedState);
			var url = AppConfig.Google.AuthUri;
			var requestParams = new NameValueCollection();
			requestParams.Add("response_type", "code");
			requestParams.Add("client_id", AppConfig.Google.ClientId);
			requestParams.Add("redirect_uri", AppConfig.Google.RedirectUri);
			requestParams.Add("state", encryptedState);
			requestParams.Add("access_type", "offline");
			requestParams.Add("prompt", "consent");
			requestParams.Add("scope", "https://www.googleapis.com/auth/drive https://www.googleapis.com/auth/userinfo.email");
			var array = (from key in requestParams.AllKeys
						 from value in requestParams.GetValues(key)
						 select string.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(value)))
				.ToArray();
			var queryString = string.Join("&", array);
			model.GoogleUrl = url + "?" + queryString;
			Session.Add("state", encryptedState);
			return View(model);

        }
		public ActionResult ChooseBoxFolder()
		{
            var model = (SimpleAutomationModel)Session["SimpleAutomationModel"];
            if (model == null)
                return RedirectToAction("Index");
            model.Destination = "box";

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
						var boxAccountRec = session.CreateCriteria<BoxAccountRec>();
						boxAccountRec.Add(Expression.Eq("AccountId", account.AccountId));
						var boxAccounts = boxAccountRec.List<BoxAccountRec>();
						if (boxAccounts.Any())
						{
							var boxAccount = boxAccounts.First();
							model.DestinationBoxAccountId = boxAccount.BoxAccountId;
							var boxTokenCrit = session.CreateCriteria<BoxTokenRec>();
							boxTokenCrit.Add(Expression.Eq("BoxTokenId", boxAccount.BoxTokenId));
							var boxTokens = boxTokenCrit.List<BoxTokenRec>();
							if (boxTokens.Any())
							{
								var boxToken = boxTokens.First();
								model.DestinationBoxTokenId = boxToken.BoxTokenId;
								return View(model);
							}
						}
					}
				}
			}
			return View(model);
		}
		public ActionResult ChooseGoogleFolder()
		{
            var model = (SimpleAutomationModel)Session["SimpleAutomationModel"];
            if (model == null)
                return RedirectToAction("Index");
            model.Destination = "google";

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
						var googleAccountRec = session.CreateCriteria<GoogleAccountRec>();
						googleAccountRec.Add(Expression.Eq("AccountId", account.AccountId));
						var googleAccounts = googleAccountRec.List<GoogleAccountRec>();
						if (googleAccounts.Any())
						{
							var googleAccount = googleAccounts.First();
							model.DestinationGoogleAccountId = googleAccount.GoogleAccountId;
							var googleTokenCrit = session.CreateCriteria<GoogleTokenRec>();
							googleTokenCrit.Add(Expression.Eq("GoogleTokenId", googleAccount.GoogleTokenId));
							var googleTokens = googleTokenCrit.List<GoogleTokenRec>();
							if (googleTokens.Any())
							{
								var googleToken = googleTokens.First();
								model.DestinationGoogleTokenId = googleToken.GoogleTokenId;
								return View(model);
							}
						}
					}
				}
			}
			return View(model);
		}
        public ActionResult ChooseBoxFolderClick(string FolderId, string FolderName, string FolderPath)
        {
            var model = (SimpleAutomationModel)Session["SimpleAutomationModel"];
			if (model == null)
				return RedirectToAction("Index");
			model.DestinationFolderId = FolderId;
			model.DestinationFolderPath = FolderPath;
			model.DestinationFolderName = FolderName;
			model.DestinationFolderLabel = FolderPath;
            if (model.TransferRuleId > 0)
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
                            var transferRuleCrit = session.CreateCriteria<TransferRuleRec>();
                            transferRuleCrit.Add(Expression.Eq("TransferRuleId", model.TransferRuleId));
                            var transferRules = transferRuleCrit.List<TransferRuleRec>();
                            if (transferRules.Any())
                            {
                                var transferRule = transferRules.First();
                                using (var transaction = session.BeginTransaction())
                                {
                                    transferRule.Destination = model.Destination;
                                    transferRule.DestinationFolderId = model.DestinationFolderId;
                                    transferRule.DestinationFolderName = model.DestinationFolderName;
                                    transferRule.DestinationFolderPath = model.DestinationFolderPath;
                                    transferRule.DestinationFolderLabel = model.DestinationFolderLabel;
                                    session.Update(transferRule);
                                    transaction.Commit();
                                }
                                return RedirectToAction("Index");
                            }
                        }
                    }
                }
            }
            return RedirectToAction("SelectLogsAndContent");
        }
        public ActionResult ChooseGoogleFolderClick(string FolderId, string FolderName, string FolderPath)
        {
            var model = (SimpleAutomationModel)Session["SimpleAutomationModel"];
			if (model == null)
				return RedirectToAction("Index");
			model.DestinationFolderId = FolderId;
			model.DestinationFolderPath = FolderPath;
			model.DestinationFolderName = FolderName;
			model.DestinationFolderLabel = FolderPath;
            if (model.TransferRuleId > 0)
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
                            var transferRuleCrit = session.CreateCriteria<TransferRuleRec>();
                            transferRuleCrit.Add(Expression.Eq("TransferRuleId", model.TransferRuleId));
                            var transferRules = transferRuleCrit.List<TransferRuleRec>();
                            if (transferRules.Any())
                            {
                                var transferRule = transferRules.First();
                                using (var transaction = session.BeginTransaction())
                                {
                                    transferRule.Destination = model.Destination;
                                    transferRule.DestinationFolderId = model.DestinationFolderId;
                                    transferRule.DestinationFolderName = model.DestinationFolderName;
                                    transferRule.DestinationFolderPath = model.DestinationFolderPath;
                                    transferRule.DestinationFolderLabel = model.DestinationFolderLabel;
                                    session.Update(transferRule);
                                    transaction.Commit();
                                }
                                return RedirectToAction("Index");
                            }
                        }
                    }
                }
            }
            return RedirectToAction("SelectLogsAndContent");
		}
        public ActionResult UseAmazon(int transferRuleId = 0)
        {
            SimpleAutomationModel model = null;
            if (transferRuleId > 0)
            {
                model = getModelFromDatabase();
                if (model != null)
                    Session["SimpleAutomationModel"] = model;
            }
            else
            {
                model = (SimpleAutomationModel)Session["SimpleAutomationModel"];
            }
            if (model == null)
                return RedirectToAction("Index");
            model.Destination = "amazon";

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
                        var amazonAccountRec = session.CreateCriteria<AmazonAccountRec>();
                        amazonAccountRec.Add(Expression.Eq("AccountId", account.AccountId));
                        var amazonAccounts = amazonAccountRec.List<AmazonAccountRec>();
                        if (amazonAccounts.Any())
                        {
                            var amazonAccount = amazonAccounts.First();
                            var authorizeChecker = new AmazonActions.AuthChecker(User.Identity.RingCloneIdentity().RingCentralId, amazonAccount.AmazonAccountId);
                            authorizeChecker.Execute();
                            if (authorizeChecker.IsAuthenticated)
                            {
                                var amazonTokenCrit = session.CreateCriteria<AmazonUserRec>();
                                amazonTokenCrit.Add(Expression.Eq("AmazonUserId", amazonAccount.AmazonUserId));
                                var amazonTokens = amazonTokenCrit.List<AmazonUserRec>();
                                if (amazonTokens.Any())
                                {
                                    var amazonToken = amazonTokens.First();
                                    model.AmazonDisplayName = amazonToken.DisplayName;
                                    model.AmazonAccountId = amazonAccount.AmazonAccountId;
                                    return RedirectToAction("ChooseAmazonFolder", model);
                                }
                            }
                        }
                    }
                }
            }
            return View(model);
        }
        [HttpPost]
        public ActionResult UseAmazon(string AccessKeyId, string SecretAccessKey)
        {
            var model = (SimpleAutomationModel)Session["SimpleAutomationModel"];
            if (model == null)
                return RedirectToAction("Index");
            model.AmazonAccessKeyId = AccessKeyId;
            model.AmazonSecretAccessKey = SecretAccessKey;
            return RedirectToAction("ValidateAmazon");
        }
        public ActionResult ValidateAmazon()
        {
            var model = (SimpleAutomationModel)Session["SimpleAutomationModel"];
            if (model == null)
                return RedirectToAction("Index");
            return View(model);
        }
        [HttpPost]
        public ActionResult ValidateAmazon(bool validated, string displayName)
        {
            var model = (SimpleAutomationModel)Session["SimpleAutomationModel"];
            if (model == null)
                return RedirectToAction("Index");
            if (validated)
            {
                model.AmazonDisplayName = displayName;
                saveAmazonUser(model);
                return RedirectToAction("ChooseAmazonFolder", model);
            }
            else
            {
                return RedirectToAction("UseAmazon");
            }
        }
        public ActionResult ChooseAmazonFolder()
        {
            var model = (SimpleAutomationModel)Session["SimpleAutomationModel"];
            if (model == null)
                return RedirectToAction("Index");
            return View(model);
        }
        public ActionResult ChooseAmazonFolderClick(string OwnerName, string BucketName, string Key, string FolderName)
        {
            var model = (SimpleAutomationModel)Session["SimpleAutomationModel"];
            if (model == null)
                return RedirectToAction("Index");
            model.DestinationBucketName = BucketName;
            model.DestinationPrefix = Key;
            model.DestinationFolderName = FolderName;
            model.DestinationFolderLabel = model.AmazonDisplayName + "/" + model.DestinationBucketName + "/" + model.DestinationPrefix;
            if (model.TransferRuleId > 0)
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
                            var transferRuleCrit = session.CreateCriteria<TransferRuleRec>();
                            transferRuleCrit.Add(Expression.Eq("TransferRuleId", model.TransferRuleId));
                            var transferRules = transferRuleCrit.List<TransferRuleRec>();
                            if (transferRules.Any())
                            {
                                var transferRule = transferRules.First();
                                using (var transaction = session.BeginTransaction())
                                {
                                    transferRule.Destination = model.Destination;
                                    transferRule.DestinationBucketName = model.DestinationBucketName;
                                    transferRule.DestinationPrefix = model.DestinationPrefix;
                                    transferRule.DestinationFolderName = model.DestinationFolderName;
                                    transferRule.DestinationFolderLabel = model.DestinationFolderLabel;
                                    session.Update(transferRule);
                                    transaction.Commit();
                                }
                                return RedirectToAction("Index");
                            }
                        }
                    }
                }
            }
            return RedirectToAction("SelectLogsAndContent");
        }
        public ActionResult SelectLogsAndContent()
        {
            var model = (SimpleAutomationModel)Session["SimpleAutomationModel"];
            if (model == null)
                model = new SimpleAutomationModel();
            Session["SimpleAutomationModel"] = model;
            return View(model);
        }
        [HttpPost]
        public ActionResult SelectLogsAndContent(string voiceLogInd, string voiceContentInd, string faxLogInd, string faxContentInd, string smsLogInd, string smsContentInd)
        {
            var model = (SimpleAutomationModel)Session["SimpleAutomationModel"];
            if (model == null)
                return RedirectToAction("Index");
            model.VoiceLogInd = !string.IsNullOrEmpty(voiceLogInd);
            model.VoiceContentInd = !string.IsNullOrEmpty(voiceContentInd);
            model.FaxLogInd = !string.IsNullOrEmpty(faxLogInd);
            model.FaxContentInd = !string.IsNullOrEmpty(faxContentInd);
            model.SmsLogInd = !string.IsNullOrEmpty(smsLogInd);
            model.SmsContentInd = !string.IsNullOrEmpty(smsContentInd);
            return RedirectToAction("PutInDatedSubFolder");
        }
        public ActionResult PutInDatedSubFolder()
        {
            var model = (SimpleAutomationModel)Session["SimpleAutomationModel"];
            if (model == null)
                return RedirectToAction("Index");
            return View(model);
        }
        [HttpPost]
        public ActionResult PutInDatedSubFolder(string PutInDatedSubfolder)
        {
            var model = (SimpleAutomationModel)Session["SimpleAutomationModel"];
            if (model == null)
                return RedirectToAction("Index");
            model.PutInDatedSubFolder = (PutInDatedSubfolder == "on");
            return RedirectToAction("Confirm");
        }
        public ActionResult Confirm()
		{
			var model = (SimpleAutomationModel)Session["SimpleAutomationModel"];
			if (model == null)
				return RedirectToAction("Index");
			return View(model);
		}
		[HttpPost]
		public ActionResult Confirm(SimpleAutomationModel model)
		{
			model = (SimpleAutomationModel)Session["SimpleAutomationModel"];
			if (model == null)
				return RedirectToAction("Index");

			using (ISessionFactory sessionFactory = NHibernateHelper.CreateSessionFactory())
			{
				using (var session = sessionFactory.OpenSession())
				{
					TransferRuleRec transferRule = null;
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
							transferRule = transferRules.First();
						}
						else
						{
							transferRule = new TransferRuleRec();
						}
						using (var transaction = session.BeginTransaction())
						{
							transferRule.AccountId = account.AccountId;
							transferRule.ActiveInd = true;
							transferRule.DayOf = "";
							transferRule.DeletedInd = false;

							transferRule.Destination = model.Destination;
							transferRule.DestinationBoxAccountId = model.DestinationBoxAccountId;
							transferRule.DestinationFolderId = model.DestinationFolderId;
                            transferRule.DestinationFolderPath = model.DestinationFolderPath;
							transferRule.DestinationFolderName = model.DestinationFolderName;
							transferRule.DestinationFolderLabel = model.DestinationFolderLabel;
							transferRule.DestinationFtpAccountId = model.DestinationFtpAccountId;
							transferRule.DestinationGoogleAccountId = model.DestinationGoogleAccountId;
                            transferRule.DestinationAmazonAccountId = model.AmazonAccountId;
                            transferRule.DestinationBucketName = model.DestinationBucketName;
                            transferRule.DestinationPrefix = model.DestinationPrefix;
							transferRule.PutInDatedSubFolder = model.PutInDatedSubFolder;

                            transferRule.VoiceLogInd = model.VoiceLogInd;
                            transferRule.VoiceContentInd = model.VoiceContentInd;
                            transferRule.FaxLogInd = model.FaxLogInd;
                            transferRule.FaxContentInd = model.FaxContentInd;
                            transferRule.SmsLogInd = model.SmsLogInd;
                            transferRule.SmsContentInd = model.SmsContentInd;

                            transferRule.Frequency = "Every day";
							transferRule.TimeOfDay = "0700";

							session.Save(transferRule);
							transaction.Commit();

						}

					}
				}
			}

            if (Session["SimpleAutomationReturnUrl"] != null && !string.IsNullOrWhiteSpace(Session["SimpleAutomationReturnUrl"].ToString()))
                return Redirect(Session["SimpleAutomationReturnUrl"].ToString());
            return RedirectToAction("Index", "SimpleAutomation");
		}

		public ActionResult BoxAuthenticated(SimpleAutomationModel model)
        {
			Session["SimpleAutomationModel"] = model;
			if (model.DestinationBoxTokenId > 0)
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
                            var account = accounts.First();
                            var boxAccountCrit = session.CreateCriteria<BoxAccountRec>();
                            boxAccountCrit.Add(Expression.Eq("AccountId", account.AccountId));
                            var boxAccounts = boxAccountCrit.List<BoxAccountRec>();
                            if (boxAccounts.Any())
                            {
                                using (var transaction = session.BeginTransaction())
                                {
                                    var boxAccount = boxAccounts.First();
                                    boxAccount.BoxTokenId = model.DestinationBoxTokenId;
                                    session.SaveOrUpdate(boxAccount);
                                    transaction.Commit();
                                    return RedirectToAction("ChooseBoxFolder");
                                }
                            }
                            else
                            {
                                using (var transaction = session.BeginTransaction())
                                {
                                    var boxAccount = new BoxAccountRec();
                                    boxAccount.AccountId = account.AccountId;
                                    boxAccount.BoxTokenId = model.DestinationBoxTokenId;
                                    session.SaveOrUpdate(boxAccount);
                                    transaction.Commit();
                                    return RedirectToAction("ChooseBoxFolder");
                                }
                            }
                        }
                    }
                }
            }
            throw new WebException("Unable to authenticate");
        }

        public ActionResult GoogleAuthenticated(SimpleAutomationModel model)
        {
			Session["SimpleAutomationModel"] = model;
			if (model.DestinationGoogleTokenId > 0)
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
                            var account = accounts.First();
                            var googleAccountCrit = session.CreateCriteria<GoogleAccountRec>();
                            googleAccountCrit.Add(Expression.Eq("AccountId", account.AccountId));
                            var googleAccounts = googleAccountCrit.List<GoogleAccountRec>();
                            if (googleAccounts.Any())
                            {
                                using (var transaction = session.BeginTransaction())
                                {
                                    var googleAccount = googleAccounts.First();
                                    googleAccount.GoogleTokenId = model.DestinationGoogleTokenId;
                                    session.SaveOrUpdate(googleAccount);
                                    transaction.Commit();
                                    return RedirectToAction("ChooseGoogleFolder");
                                }
                            }
                            else
                            {
                                using (var transaction = session.BeginTransaction())
                                {
                                    var googleAccount = new GoogleAccountRec();
                                    googleAccount.AccountId = account.AccountId;
                                    googleAccount.GoogleTokenId = model.DestinationGoogleTokenId;
                                    session.SaveOrUpdate(googleAccount);
                                    transaction.Commit();
                                    return RedirectToAction("ChooseGoogleFolder");
                                }
                            }
                        }
                    }
                }
            }
            throw new WebException("Unable to authenticate");
        }

        private SimpleAutomationModel getModelFromDatabase()
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
                        var transferRuleCrit = session.CreateCriteria<TransferRuleRec>();
                        transferRuleCrit.Add(Expression.Eq("AccountId", accountRec.AccountId));
                        transferRuleCrit.Add(Expression.Eq("DeletedInd", false));
                        var transferRules = transferRuleCrit.List<TransferRuleRec>();
                        if (transferRules.Any())
                        {
                            var transferRule = transferRules.First();
                            var model = new SimpleAutomationModel();
                            model.TransferRuleId = transferRule.TransferRuleId;
                            model.ActiveInd = transferRule.ActiveInd;
                            model.Destination = transferRule.Destination;
                            model.DestinationBoxAccountId = transferRule.DestinationBoxAccountId;
                            model.DestinationFolderId = transferRule.DestinationFolderId;
                            model.DestinationFolderPath = transferRule.DestinationFolderPath;
                            model.DestinationFolderName = transferRule.DestinationFolderName;
                            model.DestinationFolderLabel = transferRule.DestinationFolderLabel;
                            model.DestinationFtpAccountId = transferRule.DestinationFtpAccountId;
                            model.DestinationGoogleAccountId = transferRule.DestinationGoogleAccountId;
                            model.AmazonAccountId = transferRule.DestinationAmazonAccountId;
                            model.DestinationBucketName = transferRule.DestinationBucketName;
                            model.DestinationPrefix = transferRule.DestinationPrefix;
                            model.PutInDatedSubFolder = transferRule.PutInDatedSubFolder;

                            model.VoiceLogInd = transferRule.VoiceLogInd;
                            model.VoiceContentInd = transferRule.VoiceContentInd;
                            model.FaxLogInd = transferRule.FaxLogInd;
                            model.FaxContentInd = transferRule.FaxContentInd;
                            model.SmsLogInd = transferRule.SmsLogInd;
                            model.SmsContentInd = transferRule.SmsContentInd;

                            model.Frequency = transferRule.Frequency;
                            model.TimeOfDay = transferRule.TimeOfDay;

                            return model;
                        }
                    }
                }
            }
            return null;
        }
        private BoxStateModel createBoxStateModel(SimpleAutomationModel model)
        {
            return new BoxStateModel()
            {
                AccountType = "box",
                ControllerName = "SimpleAutomation",
                User = User.Identity.RingCloneIdentity().RingCentralId,
                TransferRuleId = model.TransferRuleId
            };
        }
        private GoogleStateModel createGoogleStateModel(SimpleAutomationModel model)
        {
            return new GoogleStateModel()
            {
                AccountType = "google",
                ControllerName = "SimpleAutomation",
                User = User.Identity.RingCloneIdentity().RingCentralId,
                TransferRuleId = model.TransferRuleId
            };
        }

        private bool validateAmazonKeys(SimpleAutomationModel model)
        {
            var response = AmazonActions.Helpers.Validate(model.AmazonAccessKeyId, model.AmazonSecretAccessKey);
            if (response.Validated)
            {
                model.AmazonDisplayName = response.DisplayName;
                return true;
            }
            return false;
        }
        private void saveAmazonUser(SimpleAutomationModel model)
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
                                amazonUser.AccessKeyId = model.AmazonAccessKeyId;
                                amazonUser.SecretAccessKey = model.AmazonSecretAccessKey;
                                amazonUser.DisplayName = model.AmazonDisplayName;
                                session.Save(amazonUser);
                                amazonAccount.AmazonUserId = amazonUser.AmazonUserId;
                                session.Save(amazonAccount);
                            }
                            else
                            {
                                var amazonUserCrit = session.CreateCriteria<AmazonUserRec>();
                                amazonUserCrit.Add(Expression.Eq("AmazonUserId", amazonAccount.AmazonUserId));
                                var amazonUsers = amazonCrit.List<AmazonUserRec>();
                                if (amazonUsers.Any())
                                {
                                    var amazonUser = amazonUsers.First();
                                    amazonUser.AccessKeyId = model.AmazonAccessKeyId;
                                    amazonUser.SecretAccessKey = model.AmazonSecretAccessKey;
                                    amazonUser.DisplayName = model.AmazonDisplayName;
                                    session.Save(amazonUser);
                                }
                                else
                                {
                                    var amazonUser = new AmazonUserRec();
                                    amazonUser.AccessKeyId = model.AmazonAccessKeyId;
                                    amazonUser.SecretAccessKey = model.AmazonSecretAccessKey;
                                    amazonUser.DisplayName = model.AmazonDisplayName;
                                    session.Save(amazonUser);
                                    amazonAccount.AmazonUserId = amazonUser.AmazonUserId;
                                    session.Save(amazonAccount);
                                }

                            }
                            transaction.Commit();
                            model.AmazonAccountId = amazonAccount.AmazonAccountId;
                        }
                    }
                }
            }
        }
        private string serializeDestination(AdHocTransferModel.DestinationType type)
        {
            if (type == AdHocTransferModel.DestinationType.Google)
                return "google";
            if (type == AdHocTransferModel.DestinationType.Box)
                return "box";
            if (type == AdHocTransferModel.DestinationType.Amazon)
                return "amazon";
            if (type == AdHocTransferModel.DestinationType.Ftp)
                return "ftp";
            return "error";
        }
        #region Database Models

        private class BoxStateModel
        {
            public string User { get; set; }
            public string ControllerName { get; set; }
            public string AccountType { get; set; }
            public int TransferRuleId { get; set; }
        }
        private class GoogleStateModel
        {
            public string User { get; set; }
            public string ControllerName { get; set; }
            public string AccountType { get; set; }
            public int TransferRuleId { get; set; }
        }


        private class AccountRec
        {
            public virtual int AccountId { get; set; }
            public virtual string RingCentralId { get; set; }
			public virtual string PlanId { get; set; }
			public virtual string StripeCustomerId { get; set; }
			public virtual string StripeSubscriptionId { get; set; }
			public virtual bool PaymentIsCurrentInd { get; set; }
		}
		private class AccountRecMap : ClassMap<AccountRec>
        {
            public AccountRecMap()
            {
                Table("T_ACCOUNT");
                Id(x => x.AccountId).Column("AccountId");
                Map(x => x.RingCentralId);
				Map(x => x.PlanId);
				Map(x => x.StripeCustomerId);
				Map(x => x.StripeSubscriptionId);
			}
		}
        private class BoxAccountRec
        {
            public virtual int BoxAccountId { get; set; }
            public virtual int AccountId { get; set; }
            public virtual int BoxTokenId { get; set; }
        }
        private class BoxAccountRecMap : ClassMap<BoxAccountRec>
        {
            public BoxAccountRecMap()
            {
                Table("T_BOXACCOUNT");
                Id(x => x.BoxAccountId).Column("BoxAccountId");
                Map(x => x.AccountId);
                Map(x => x.BoxTokenId);
            }
        }
        private class BoxTokenRec
        {
            public virtual int BoxTokenId { get; set; }
            public virtual string AccessToken { get; set; }
            public virtual string ExpiresIn { get; set; }
            public virtual string TokenType { get; set; }
            public virtual string RefreshToken { get; set; }
            public virtual DateTime LastRefreshedOn { get; set; }
            public virtual bool DeletedInd { get; set; }
            public virtual string Email { get; set; }
        }
        private class BoxTokenRecMap : ClassMap<BoxTokenRec>
        {
            public BoxTokenRecMap()
            {
                Table("T_BOXTOKEN");
                Id(x => x.BoxTokenId);
                Map(x => x.AccessToken);
                Map(x => x.ExpiresIn);
                Map(x => x.TokenType);
                Map(x => x.RefreshToken);
                Map(x => x.DeletedInd);
                Map(x => x.LastRefreshedOn);
                Map(x => x.Email);
            }
        }
        private class GoogleAccountRec
        {
            public virtual int GoogleAccountId { get; set; }
            public virtual int AccountId { get; set; }
            public virtual int GoogleTokenId { get; set; }
        }
        private class GoogleAccountRecMap : ClassMap<GoogleAccountRec>
        {
            public GoogleAccountRecMap()
            {
                Table("T_GOOGLEACCOUNT");
                Id(x => x.GoogleAccountId).Column("GoogleAccountId");
                Map(x => x.AccountId);
                Map(x => x.GoogleTokenId);
            }
        }
        private class GoogleTokenRec
        {
            public virtual int GoogleTokenId { get; set; }
            public virtual string AccessToken { get; set; }
            public virtual string ExpiresIn { get; set; }
            public virtual string TokenType { get; set; }
            public virtual string RefreshToken { get; set; }
            public virtual DateTime LastRefreshedOn { get; set; }
            public virtual bool DeletedInd { get; set; }
            public virtual string Email { get; set; }
        }
        private class GoogleTokenRecMap : ClassMap<GoogleTokenRec>
        {
            public GoogleTokenRecMap()
            {
                Table("T_GOOGLETOKEN");
                Id(x => x.GoogleTokenId);
                Map(x => x.AccessToken);
                Map(x => x.ExpiresIn);
                Map(x => x.TokenType);
                Map(x => x.RefreshToken);
                Map(x => x.DeletedInd);
                Map(x => x.LastRefreshedOn);
                Map(x => x.Email);
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
            public virtual int DestinationAmazonAccountId { get; set; }
            public virtual string DestinationBucketName { get; set; }
            public virtual string DestinationPrefix { get; set; }
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
                Map(x => x.DestinationAmazonAccountId);
                Map(x => x.DestinationBucketName);
                Map(x => x.DestinationPrefix);
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
