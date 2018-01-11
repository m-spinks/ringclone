using Dapper;
using RingClone.Portal.Filters;
using RingClone.Portal.Helpers;
using RingClone.Portal.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace RingClone.Portal.Controllers
{
	public class LogController : Controller
	{
        [Authorize]
        [Register]
        public ActionResult Index()
		{
            var model = new LogViewModel();
            model.DateFrom = DateTime.Now.ToUniversalTime().AddDays(-90);
            model.DateTo = DateTime.Now.ToUniversalTime();
            model.Type = "voice";

            using (IDbConnection db = new SqlConnection(ConnectionStringHelper.ConnectionString))
            {
                var accounts = db.Query<Account>("SELECT AccountId,RingCentralId,PlanId,CancelledInd FROM T_ACCOUNT WHERE RINGCENTRALID = @ringCentralId", new { ringCentralId = User.Identity.RingCloneIdentity().RingCentralId });
                if (accounts.Any())
                {
                    var account = accounts.First();
                    var paidPlans = new string[] { "ringclone_silver", "ringclone_gold", "ringclone_platinum" };
                    model.HasPaidAccount = !string.IsNullOrWhiteSpace(account.PlanId) && paidPlans.Any(x => x == account.PlanId) && !account.CancelledInd;
                    var transferRules = db.Query<TransferRule>("SELECT TransferRuleId,AccountId,DeletedInd,ActiveInd FROM T_TRANSFERRULE WHERE ACCOUNTID = @accountId", new { accountId = account.AccountId });
                    model.HasAutomation = transferRules != null && transferRules.Any(x => !x.DeletedInd && x.ActiveInd);
                }
            }
            return View(model);
		}

        [Authorize]
        [Register]
        public ActionResult Archive()
        {
            var model = new ArchiveViewModel();
            model.DateFrom = DateTime.Now.ToUniversalTime().AddDays(-90);
            model.DateTo = DateTime.Now.ToUniversalTime();
            model.Type = "voice";
            model.PageNumber = 1;

            using (IDbConnection db = new SqlConnection(ConnectionStringHelper.ConnectionString))
            {
                var accounts = db.Query<Account>("SELECT AccountId,RingCentralId,PlanId,CancelledInd FROM T_ACCOUNT WHERE RINGCENTRALID = @ringCentralId", new { ringCentralId = User.Identity.RingCloneIdentity().RingCentralId });
                if (accounts.Any())
                {
                    var account = accounts.First();
                    var paidPlans = new string[] { "ringclone_silver", "ringclone_gold", "ringclone_platinum" };
                    model.HasPaidAccount = !string.IsNullOrWhiteSpace(account.PlanId) && paidPlans.Any(x => x == account.PlanId) && !account.CancelledInd;
                    var transferRules = db.Query<TransferRule>("SELECT TransferRuleId,AccountId,DeletedInd,ActiveInd FROM T_TRANSFERRULE WHERE ACCOUNTID = @accountId", new { accountId = account.AccountId });
                    model.HasAutomation = transferRules != null && transferRules.Any(x => !x.DeletedInd && x.ActiveInd);
                }
            }
            return View(model);
        }

        #region Database Models

        private class Account
        {
            public int AccountId;
            public string RingCentralId;
            public string PlanId;
            public bool CancelledInd;
        }
        private class TransferRule
        {
            public int TransferRuleId;
            public int AccountId;
            public bool DeletedInd;
            public bool ActiveInd;
        }

#endregion

    }
}
