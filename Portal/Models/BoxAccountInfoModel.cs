using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RingClone.Portal.Models
{
	public class BoxAccountInfoModel
	{
        public int BoxAccountId;
        public bool HasBoxAccount;
        public bool AbleToConnectToBoxAccount;
        public string BoxAccountEmail;
    }
}