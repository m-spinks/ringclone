﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RingClone.Portal.Controllers
{
    public class DownloadController : Controller
    {
        // GET: Download
        public ActionResult Index(string id)
        {
            return View();
        }
    }
}