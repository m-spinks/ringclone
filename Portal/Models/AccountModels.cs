using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Globalization;
using System.Web.Security;

namespace RingClone.Portal.Models
{
	public class LoginModel
	{
		//[RegularExpression("([1-9][0-9]*)", ErrorMessage = "Use only numeric digits without spaces or special characters")]
		[StringLength(18, MinimumLength = 10, ErrorMessage = "Invalid account number")]
		public virtual string RingCentralId { get; set; }
		public virtual string RingCentralPassword { get; set; }
        public virtual bool IsFirstTimeLogin { get; set; }
		public bool RememberMe { get; set; }
	}

    public class ChangePlanModel
    {
        public string ExistingPlanId;
        public string NewPlanId;
        public string BillingEmail;
        public bool PaymentFailed;
    }
    public class ReenableModel
	{
		public string NewPlanId;
	}
}
