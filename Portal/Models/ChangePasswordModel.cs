using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RingClone.Portal.Models
{
	public class ChangePasswordModel
	{
		public virtual string NewPassword { get; set; }
		public virtual string ConfirmPassword { get; set; }
	}
}