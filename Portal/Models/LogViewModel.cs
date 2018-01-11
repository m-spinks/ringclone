using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RingClone.Portal.Models
{
	public class LogViewModel
	{
        public string SubscriptionPlan { get; set; }
        public string ExtensionNumber { get; set; }
        public DateTime? DateFrom { get; set; }
		public DateTime? DateTo { get; set; }
        public string Type { get; set; }
        public bool HasPaidAccount { get; set; }
        public bool HasAutomation { get; set; }
	}
}