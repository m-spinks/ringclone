using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RingClone.Portal.Models
{
	public class GoogleAccountInfoModel
	{
        public int GoogleAccountId;
        public bool HasGoogleAccount;
        public bool AbleToConnectToGoogleAccount;
        public string GoogleAccountEmail;
    }
}