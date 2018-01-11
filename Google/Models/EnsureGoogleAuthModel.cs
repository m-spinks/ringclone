using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleActions.Models
{
	public class EnsureGoogleAuthModel
	{
		public int GoogleTokenId { get; set; }
		public string AccessToken { get; set; }
		public string ExpiresIn { get; set; }
		public string TokenType { get; set; }
		public string RefreshToken { get; set; }
		public DateTime LastRefreshedOn { get; set; }
		public bool DeletedInd { get; set; }
		public bool Changed { get; set; }
	}
}
