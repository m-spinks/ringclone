using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RingClone.Portal.Models
{
	public class FtpConnectionModel
	{
		public bool HasFtpAccount;
		public bool CanLogin;
		public int FtpAccountId;
		public string FtpAccountUri;
		public string FtpAccountUsername;
		public string FtpAccountPassword;
	}
}