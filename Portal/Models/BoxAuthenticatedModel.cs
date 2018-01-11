using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace RingClone.Portal.Models
{
	public class BoxAuthenticatedModel
	{
		public string Code { get; set; }
		public string State { get; set; }
		public string ModelName { get; set; }
		public string Email { get; set; }
		public string AccessToken { get; set; }
		public string ExpiresIn { get; set; }
		public string TokenType { get; set; }
		public string RefreshToken { get; set; }
		public int BoxTokenId { get; set; }
	}
}