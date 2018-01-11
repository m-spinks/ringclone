using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace RingClone.Portal.Models
{
	public class TransferRuleEditModel
	{
		public virtual int TransferRuleId { get; set; }

		public virtual int FtpAccountId { get; set; }
	
		public virtual int AccountId { get; set; }
		
		[DisplayFormat(ConvertEmptyStringToNull = false)]
		public virtual string Destination { get; set; }
		
		public virtual string Frequency { get; set; }
		
		[DisplayFormat(ConvertEmptyStringToNull = false)]
		public virtual string Source { get; set; }
		
		public virtual string TimeOfDay { get; set; }
	}
}