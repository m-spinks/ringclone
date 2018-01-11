using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace RingClone.Portal
{
	public class RouteConfig
	{
		public static void RegisterRoutes(RouteCollection routes)
		{
			routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "CreateSubscriptionRoute",
                url: "payment/createsubscription/{plan}",
                defaults: new { controller = "Payment", action = "CreateSubscription" }
            );
			routes.MapRoute(
				name: "TransferBatchRoute",
				url: "transferbatch/{id}/{action}",
				defaults: new { controller = "TransferBatch", action = "Status", id = UrlParameter.Optional }
			);
			//routes.MapRoute(
			//	name: "SignupRoute",
			//	url: "signup/{id}/{action}",
			//	defaults: new { controller = "Signup", action = "Index", id = RingClone.Portal.Helpers.SubscriptionHelper.SubscriptionPlans.First().Id }
			//);
			routes.MapRoute(
				name: "Default",
				url: "{controller}/{action}/{id}",
				defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
			);
		}
	}
}