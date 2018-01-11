using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RingClone.Portal.Models
{
	public class RegisterModel
	{
		public bool AlreadyExists { get; set; }
        public string PlanId { get; set; }
        public string BillingEmail { get; set; }
        public bool PaymentFailed { get; set; }
    }
}