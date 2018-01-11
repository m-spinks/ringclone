using RingClone.Portal.Filters;
using RingClone.Portal.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RingClone.Portal.Controllers
{
    [Register]
    [Authorize]
    public class HistoryController : Controller
    {

        public ActionResult Index()
        {
			var model = Helpers.HistoryHelper.GenerateHistory(User.Identity.RingCloneIdentity().RingCentralId, 30);
            return View(model);
        }

    }
}
