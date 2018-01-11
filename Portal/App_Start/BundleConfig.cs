using System.Web;
using System.Web.Optimization;

namespace RingClone.Portal
{
	public class BundleConfig
	{
		// For more information on Bundling, visit http://go.microsoft.com/fwlink/?LinkId=254725
		public static void RegisterBundles(BundleCollection bundles)
		{
            bundles.Add(new ScriptBundle("~/content/scripts")
                .Include("~/content/jquery-1.12.0/jquery-1.12.0.js")
                .Include("~/content/jquery-loadmask-0.4/jquery.loadmask.js")
                .Include("~/content/bootstrap/js/bootstrap.js")
                .Include("~/content/formvalidation-dist-v0.7.0/dist/js/formValidation.js")
				.Include("~/content/formvalidation-dist-v0.7.0/dist/js/framework/bootstrap.js")
				.Include("~/content/bootstrap-notify/bootstrap-notify.js")
				.Include("~/content/bootstrap-datepicker/js/bootstrap-datepicker.js")
				.Include("~/scripts/help.js")
                .Include("~/scripts/downloads.js")
                );

            bundles.Add(new StyleBundle("~/content/styles")
                .Include("~/Content/bootstrap/css/bootstrap.min.css", new CssRewriteUrlTransform())
                .Include("~/Content/formvalidation-dist-v0.7.0/dist/css/formValidation.css")
                .Include("~/Content/jquery-loadmask-0.4/jquery.loadmask.css")
				.Include("~/content/bootstrap-datepicker/css/bootstrap-datepicker.css")
				.Include("~/content/animate/animate.css")
				.Include("~/Content/site.css"));

            bundles.Add(new StyleBundle("~/content/pricing-styles")
                .Include("~/Content/pricing.css"));

        }
    }
}