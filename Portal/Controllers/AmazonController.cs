using FluentNHibernate.Mapping;
using NHibernate;
using RingClone.Portal.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RingClone.Portal.Helpers;
using NHibernate.Criterion;

namespace RingClone.Portal.Controllers
{
    public class AmazonController : Controller
    {
        [NotCancelled]
        public ActionResult Setup()
        {
            ViewBag.AccessKeyId = "";
            ViewBag.SecretAccessKey = "";
            ViewBag.DisplayName = "";
            using (ISessionFactory sessionFactory = NHibernateHelper.CreateSessionFactory())
            {
                using (var session = sessionFactory.OpenSession())
                {
                    var accountCrit = session.CreateCriteria<AccountRecord>();
                    accountCrit.Add(Expression.Eq("RingCentralId", User.Identity.RingCloneIdentity().RingCentralId));
                    var accounts = accountCrit.List<AccountRecord>();
                    if (accounts.Any())
                    {
                        using (var transaction = session.BeginTransaction())
                        {
                            AmazonAccountRecord amazonAccount;
                            var account = accounts.First();
                            var amazonCrit = session.CreateCriteria<AmazonAccountRecord>();
                            amazonCrit.Add(Expression.Eq("AccountId", account.AccountId));
                            var amazonAccounts = amazonCrit.List<AmazonAccountRecord>();
                            if (amazonAccounts.Any())
                            {
                                amazonAccount = amazonAccounts.First();
                                var amazonUserCrit = session.CreateCriteria<AmazonUserRecord>();
                                amazonUserCrit.Add(Expression.Eq("AmazonUserId", amazonAccount.AmazonUserId));
                                var amazonUsers = amazonUserCrit.List<AmazonUserRecord>();
                                if (amazonUsers.Any())
                                {
                                    var amazonUser = amazonUsers.First();
                                    ViewBag.AccessKeyId = amazonUser.AccessKeyId;
                                    ViewBag.SecretAccessKey = amazonUser.SecretAccessKey;
                                    ViewBag.DisplayName = amazonUser.DisplayName;
                                    return View();
                                }
                            }
                        }
                    }
                }
            }
            return View();
        }
        [HttpPost]
        public ActionResult Setup(string accessKeyId, string secretAccessKey)
        {
            return RedirectToAction("ValidateAmazon", new {accessKeyId = accessKeyId, secretAccessKey = secretAccessKey});
        }
        public ActionResult ValidateAmazon(string accessKeyId, string secretAccessKey)
        {
            ViewBag.AccessKeyId = accessKeyId;
            ViewBag.SecretAccessKey = secretAccessKey;
            return View();
        }
        [HttpPost]
        public ActionResult ValidateAmazon(bool validated, string displayName, string accessKeyId, string secretAccessKey)
        {
            if (validated)
            {
                return RedirectToAction("Index", "Connections");
            }
            ViewBag.AccessKeyId = accessKeyId;
            ViewBag.SecretAccessKey = secretAccessKey;
            ViewBag.DisplayName = displayName;
            return View();
        }
        public ActionResult Walkthrough()
        {
            return View();
        }
        #region Database Models
        private class AccountRecord
        {
            public virtual int AccountId { get; set; }
            public virtual string RingCentralId { get; set; }
            public virtual string PlanId { get; set; }
            public virtual string StripeCustomerId { get; set; }
            public virtual string StripeSubscriptionId { get; set; }
            public virtual bool PaymentIsCurrentInd { get; set; }
        }
        private class AccountRecordMap : ClassMap<AccountRecord>
        {
            public AccountRecordMap()
            {
                Table("T_ACCOUNT");
                Id(x => x.AccountId).Column("AccountId");
                Map(x => x.RingCentralId);
                Map(x => x.PlanId);
                Map(x => x.StripeCustomerId);
                Map(x => x.StripeSubscriptionId);
            }
        }
        public class AmazonAccountRecord
        {
            public virtual int AmazonAccountId { get; set; }
            public virtual int AmazonUserId { get; set; }
            public virtual int AccountId { get; set; }
            public virtual string AmazonAccountName { get; set; }
            public virtual bool DeletedInd { get; set; }
            public virtual bool ActiveInd { get; set; }
        }
        private class AmazonAccountRecordMap : ClassMap<AmazonAccountRecord>
        {
            public AmazonAccountRecordMap()
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
        public class AmazonUserRecord
        {
            public virtual int AmazonUserId { get; set; }
            public virtual string AccessKeyId { get; set; }
            public virtual string SecretAccessKey { get; set; }
            public virtual string DisplayName { get; set; }
            public virtual bool DeletedInd { get; set; }
        }
        private class AmazonUserRecordMap : ClassMap<AmazonUserRecord>
        {
            public AmazonUserRecordMap()
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
