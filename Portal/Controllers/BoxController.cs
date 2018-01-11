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
    public class BoxController : Controller
    {

		[NotCancelled]
        public ActionResult Setup()
        {
            JavaScriptSerializer jss = new JavaScriptSerializer();
            var serializedState = jss.Serialize(createBoxStateModel());
            var encryptedState = Helpers.EncryptedString.DatabaseEncrypt(serializedState);
            var url = AppConfig.Box.AuthUri;
            var requestParams = new NameValueCollection();
            requestParams.Add("response_type", "code");
            requestParams.Add("client_id", AppConfig.Box.ClientId);
            requestParams.Add("redirect_uri", AppConfig.Box.RedirectUri);
            requestParams.Add("state", encryptedState);
            var array = (from key in requestParams.AllKeys
                         from value in requestParams.GetValues(key)
                         select string.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(value)))
                .ToArray();
            var queryString = string.Join("&", array);
            ViewBag.BoxUrl = url + "?" + queryString;
            Session.Add("state", encryptedState);
            return View();
        }
        private BoxStateModel createBoxStateModel()
        {
            return new BoxStateModel()
            {
                AccountType = "box",
                ControllerName = "Box",
                User = User.Identity.RingCloneIdentity().RingCentralId
            };
        }

        private class BoxStateModel
        {
            public string User { get; set; }
            public string ControllerName { get; set; }
            public string AccountType { get; set; }
        }
    }
}
