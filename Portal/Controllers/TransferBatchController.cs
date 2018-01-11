using FluentNHibernate.Mapping;
using NHibernate;
using NHibernate.Criterion;
using RingClone.Portal.Helpers;
using RingClone.Portal.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace RingClone.Portal.Controllers
{
	public class TransferBatchController : Controller
	{
		public ActionResult Status(string id)
		{
			var model = TransferBatchStatusHelper.GenerateStatus(User.Identity.RingCloneIdentity().RingCentralId, int.Parse(id));
			return View(model);
		}
	}
}
