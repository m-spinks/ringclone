using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RingClone.Portal.Models
{
	public class RegistrationModel
	{
		public string RingCentralId { get; set; }
		public string RingCentralPassword { get; set; }
		public bool AlreadyExists { get; set; }
		public bool Invalid { get; set; }
        public string Plan { get; set; }
        public string BillingEmail { get; set; }
        public string RingCentralToken { get; set; }
        public string RingCentralTokenExpiresIn { get; set; }
        public string RingCentralRefreshToken { get; set; }
        public string RingCentralRefreshTokenExpiresIn { get; set; }
        public string RingCentralTokenType { get; set; }
        public string RingCentralEndpointId { get; set; }
        public string RingCentralOwnerId { get; set; }
        public string RingCentralScope { get; set; }
        public string RingCentralExtension { get; set; }
        public bool PaymentFailed { get; set; }
    }
}