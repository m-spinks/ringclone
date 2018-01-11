using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RingClone.Portal.Models
{
	public class FtpAccountEditModel
	{
		public virtual int FtpAccountId { get; set; }
		public virtual string FtpAccountName { get; set; }
		public virtual string Uri { get; set; }
		public virtual string Username { get; set; }
		public virtual string Password { get; set; }
	}
}