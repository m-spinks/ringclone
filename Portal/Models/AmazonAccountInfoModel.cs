using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RingClone.Portal.Models
{
	public class AmazonAccountInfoModel
	{
        public int AmazonAccountId;
        public bool HasAmazonAccount;
        public bool AbleToConnectToAmazonAccount;
        public string DisplayName;
    }
}