using FluentNHibernate.Mapping;
using NHibernate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace RingClone.Portal
{
	public class MvcApplication : System.Web.HttpApplication
	{
		protected void Application_Start()
		{
			AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
			RouteConfig.RegisterRoutes(RouteTable.Routes);
			BundleConfig.RegisterBundles(BundleTable.Bundles);
			AuthConfig.RegisterAuth();
        }

#if (DEBUG)

        protected void Session_Start(object sender, EventArgs e)
        {

            //var webHookCreator = new RingCentral.CreateWebhook("1934184005");
            //webHookCreator.AddEventFilter("/restapi/v1.0/account/~/extension/~/presence?detailedTelephonyState=true&sipData=true");
            //webHookCreator.AddEventFilter("/restapi/v1.0/account/~/extension/~/message-store");
            //webHookCreator.Execute();

            //var downloader = new RingCentral.ContentDownloader("1934184005", "https://media.ringcentral.com/restapi/v1.0/account/1934184005/extension/1934184005/message-store/818640453004/content/292443348004");
            //downloader.Execute();
            //System.IO.File.WriteAllBytes("D:\\temp\\testimage.jpg", downloader.data);



            //            string[] usersToRefresh = {
            //"1953727010",
            //"236147031",
            //"1258885011",
            //"291722027",
            //"1145204021",
            //"441376023",
            //"151799031",
            //"191625028",
            //"2095146011",
            //"1934184005",
            //"389105028",
            //"990042064",
            //"292009027",
            //"2104746011",
            //"450792027",
            //"400234016",
            //"1120204011",
            //};

            //System.Web.Script.Serialization.JavaScriptSerializer jss = new System.Web.Script.Serialization.JavaScriptSerializer();
            ////var userIdentity = new Models.UserIdentityModel()
            ////{
            ////    RingCentralId = "1120204011",
            ////    DisplayName = "Testing I",
            ////    Company = ""
            ////};
            ////var userIdentity = new Models.UserIdentityModel()
            ////{
            ////    RingCentralId = "400234016",
            ////    DisplayName = "Testing K",
            ////    Company = ""
            ////};
            ////var userIdentity = new Models.UserIdentityModel()
            ////{
            ////    RingCentralId = "2104746011",
            ////    DisplayName = "Rob Davis",
            ////    Company = ""
            ////};
            ////var userIdentity = new Models.UserIdentityModel()
            ////{
            ////    RingCentralId = "990042064",
            ////    DisplayName = "Test Lisa Lovett",
            ////    Company = ""
            ////};
            ////var userIdentity = new Models.UserIdentityModel()
            ////{
            ////    RingCentralId = "389105028",
            ////    DisplayName = "Test Kevin Masterson"
            ////    Company = "Cafe Rio"
            ////};
            ////var userIdentity = new Models.UserIdentityModel()
            ////{
            ////    RingCentralId = "191625028",
            ////    DisplayName = "Test User",
            ////    Company = "RingClone"
            ////};
            ////var userIdentity = new Models.UserIdentityModel()
            ////{
            ////    RingCentralId = "134636004",
            ////    DisplayName = "Super Admin",
            ////    Company = "RingClone"
            ////};
            //var userIdentity = new Models.UserIdentityModel()
            //{
            //    RingCentralId = "1934184005",
            //    DisplayName = "Test KH",
            //    Company = "Kwanza Hall for Mayor"
            //};
            ////var userIdentity = new Models.UserIdentityModel()
            ////{
            ////    RingCentralId = "151799031",
            ////    DisplayName = "Testing David Daniels",
            ////    Company = ""
            ////};
            ////var userIdentity = new Models.UserIdentityModel()
            ////{
            ////    RingCentralId = "1145204021",
            ////    DisplayName = "Test Addison Russell",
            ////    Company = ""
            ////};
            ////var userIdentity = new Models.UserIdentityModel()
            ////{
            ////    RingCentralId = "1953727010",
            ////    DisplayName = "Andy Riesenbach",
            ////    Company = ""
            ////};
            ////var userIdentity = new Models.UserIdentityModel()
            ////{
            ////    RingCentralId = "236147031",
            ////    DisplayName = "Donald Owens",
            ////    Company = ""
            ////};
            ////var userIdentity = new Models.UserIdentityModel()
            ////{
            ////    RingCentralId = "1258885011",
            ////    DisplayName = "Jeanette Carbaugh",
            ////    Company = ""
            ////};
            ////var userIdentity = new Models.UserIdentityModel()
            ////{
            ////    RingCentralId = "291722027",
            ////    DisplayName = "Morgan Gordon",
            ////    Company = ""
            ////};
            ////var userIdentity = new Models.UserIdentityModel()
            ////{
            ////    RingCentralId = "441376023",
            ////    DisplayName = "101-NY Physical Medicine",
            ////    Company = ""
            ////};
            //System.Web.Security.FormsAuthentication.SetAuthCookie(jss.Serialize(userIdentity), true);
        }

#endif

        protected void Application_Error(Object sender, EventArgs e)
		{
			var raisedException = Server.GetLastError();
			using (ISessionFactory sessionFactory = Helpers.NHibernateHelper.CreateSessionFactory())
			{
				using (var session = sessionFactory.OpenSession())
				{
					var errorRec = new ErrorRec();
					errorRec.CreateDate = DateTime.Now.ToUniversalTime();
					errorRec.Exception1 = "";
					errorRec.Exception2 = "";
					errorRec.Exception3 = "";
					errorRec.Exception4 = "";
					if (raisedException != null)
					{
						errorRec.Exception1 = raisedException.Message;
						if (raisedException.InnerException != null)
						{
							errorRec.Exception2 = raisedException.InnerException.Message;
							if (raisedException.InnerException.InnerException != null)
							{
								errorRec.Exception3 = raisedException.InnerException.InnerException.Message;
								if (raisedException.InnerException.InnerException.InnerException != null)
								{
									errorRec.Exception4 = raisedException.InnerException.InnerException.InnerException.Message;
								}
							}
						}
					}
					using (var transaction = session.BeginTransaction())
					{
						session.Save(errorRec);
						transaction.Commit();
					}
				}
			}
		}

		#region Database Mapping
		private class ErrorRec
		{
			public virtual int ErrorId { get; set; }
			public virtual DateTime CreateDate { get; set; }
			public virtual string Exception1 { get; set; }
			public virtual string Exception2 { get; set; }
			public virtual string Exception3 { get; set; }
			public virtual string Exception4 { get; set; }
		}

		private class ErrorRecMap : ClassMap<ErrorRec>
		{
			public ErrorRecMap()
			{
				Table("T_ERROR");
				Id(x => x.ErrorId);
				Map(x => x.CreateDate);
				Map(x => x.Exception1);
				Map(x => x.Exception2);
				Map(x => x.Exception3);
				Map(x => x.Exception4);
			}
		}

		#endregion

	}
}