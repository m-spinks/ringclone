using NHibernate;
using RingClone.Portal.Filters;
using RingClone.Portal.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace RingClone.Portal.Controllers
{
    public class GoogleController : Controller
    {

		[NotCancelled]
		public ActionResult Setup()
        {
            JavaScriptSerializer jss = new JavaScriptSerializer();
            var serializedState = jss.Serialize(createGoogleStateModel());
            var encryptedState = Helpers.EncryptedString.DatabaseEncrypt(serializedState);
			var url = AppConfig.Google.AuthUri;
			var requestParams = new NameValueCollection();
			requestParams.Add("response_type", "code");
			requestParams.Add("client_id", AppConfig.Google.ClientId);
			requestParams.Add("redirect_uri", AppConfig.Google.RedirectUri);
			requestParams.Add("state", encryptedState);
			requestParams.Add("access_type", "offline");
			requestParams.Add("prompt", "consent");
			requestParams.Add("scope", "https://www.googleapis.com/auth/drive https://www.googleapis.com/auth/userinfo.email");
			var array = (from key in requestParams.AllKeys
						 from value in requestParams.GetValues(key)
						 select string.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(value)))
				.ToArray();
			var queryString = string.Join("&", array);
			ViewBag.GoogleUrl = url + "?" + queryString;
			Session.Add("state", encryptedState);
			return View();
        }
        private GoogleStateModel createGoogleStateModel()
        {
            return new GoogleStateModel()
            {
                AccountType = "google",
                ControllerName = "Google",
                User = User.Identity.RingCloneIdentity().RingCentralId
            };
        }

        private class GoogleStateModel
        {
            public string User { get; set; }
            public string ControllerName { get; set; }
            public string AccountType { get; set; }
        }
    }
}
