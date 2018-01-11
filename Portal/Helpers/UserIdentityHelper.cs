using RingClone.Portal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Script.Serialization;

namespace RingClone.Portal.Helpers
{
    public static class UserIdentityHelper
    {
        public static UserIdentityModel RingCloneIdentity(this IIdentity identity)
        {
            if (identity == null)
            {
                throw new ArgumentNullException("Identity");
            }
            JavaScriptSerializer jss = new JavaScriptSerializer();
            UserIdentityModel userIdentity = jss.Deserialize<UserIdentityModel>(identity.Name.ToString());
            if (userIdentity == null)
            {
                throw new ArgumentNullException("RingClone Identity");
            }
            return userIdentity;
        }
    }
}