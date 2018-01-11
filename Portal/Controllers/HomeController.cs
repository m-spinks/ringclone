using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace RingClone.Portal.Controllers
{
	public class HomeController : Controller
	{
		[AllowAnonymous]
		public ActionResult Index(string refId = "", string refPlan = "")
		{
            Session["refid"] = refId;
            Session["refplan"] = refPlan;
            return RedirectToAction("Index", "Log");
		}
	}
}
