using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RingClone.Portal.Controllers
{
    public class ErrorController : Controller
    {
        // GET: Error
        public ActionResult Index()
        {
            if (User != null && User.Identity != null && !string.IsNullOrWhiteSpace(User.Identity.Name))
                return View("~/Views/Shared/Error.cshtml");
            return View("~/Views/Shared/ErrorPublic.cshtml");
        }
    }
}