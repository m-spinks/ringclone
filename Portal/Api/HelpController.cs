using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using RingClone.Portal.Models;
using NHibernate;
using RingClone.Portal.Helpers;
using NHibernate.Criterion;
using FluentNHibernate.Mapping;
using System.IO;
using System.Web.Script.Serialization;
using System.Collections.Specialized;
using System.Web;

namespace RingClone.Portal.Api
{
    [Authorize]
    public class HelpController : ApiController
    {
		[HttpPost]
		public bool SendMessage([FromBody]HelpMessage helpMessage)
		{
            if (helpMessage == null || string.IsNullOrEmpty(helpMessage.Message))
                return false;
            var message = helpMessage.Message.Replace("\n", "<br />") + "<br /><br />Contact Email: " + helpMessage.ContactEmail;
            string displayedUser = "Unknown User";
            if (User != null && User.Identity != null && !string.IsNullOrEmpty(User.Identity.Name) && User.Identity.RingCloneIdentity() != null && User.Identity.RingCloneIdentity().RingCentralId != null)
                displayedUser = "User " + User.Identity.RingCloneIdentity().RingCentralId;
            if (EmailHelper.SendMail("support@ringclone.com", "Message from RingClone | " + displayedUser, message, "", "") == "Success")
                return true;
            else
                return false;
		}

        public class HelpMessage
        {
			public string Message { get; set; }
			public string ContactEmail { get; set; }
		}
	}
}
