using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace RingClone.Portal.Models
{
	public class CreateNewRuleModel
	{
		public string AccountType { get; set; }
		public int GoogleAccountId { get; set; }
		public int GoogleTokenId { get; set; }
		public int BoxAccountId { get; set; }
		public int BoxTokenId { get; set; }
		public int FtpAccountId { get; set; }
		public string Email { get; set; }
		public string AccessToken { get; set; }
		public string IdToken { get; set; }
		public string ExpiresIn { get; set; }
		public string TokenType { get; set; }
		public string RefreshToken { get; set; }
		[DisplayFormat(ConvertEmptyStringToNull = false)]
		public virtual string Destination { get; set; }
		[DisplayFormat(ConvertEmptyStringToNull = false)]
		public virtual string Frequency { get; set; }
		[DisplayFormat(ConvertEmptyStringToNull = false)]
		public virtual string Source { get; set; }
		[DisplayFormat(ConvertEmptyStringToNull = false)]
		public virtual string TimeOfDay { get; set; }
		public virtual bool ActivateImmediately { get; set; }
		public virtual string Url { get; set; }
	}
}