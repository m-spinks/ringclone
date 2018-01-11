using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;

namespace RingClone.Portal.Helpers
{
	public static class CustomValidationMessage
	{
		public static MvcHtmlString BootstrapValidationMessage(this HtmlHelper helper, string validationMessage = "")
		{
			string retVal = "";
			if (helper.ViewData.ModelState.IsValid)
				return new MvcHtmlString("");
			retVal += "<div class=\"alert alert-danger\" role=\"alert\"><span>";
			if (!String.IsNullOrEmpty(validationMessage))
				retVal += helper.Encode(validationMessage);
			retVal += "</span>";
			retVal += "<div class='text'>";
			foreach (var key in helper.ViewData.ModelState.Keys)
			{
				foreach (var err in helper.ViewData.ModelState[key].Errors)
					retVal += "<p>" + helper.Encode(err.ErrorMessage) + "</p>";
			}
			retVal += "</div></div>";
			return new MvcHtmlString(retVal.ToString());
		}
	}
}