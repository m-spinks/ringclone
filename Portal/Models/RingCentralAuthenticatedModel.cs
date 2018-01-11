using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace RingClone.Portal.Models
{
	public class RingCentralAuthenticatedModel
    {
		public string code { get; set; }
		public string expires_in { get; set; }
		public string state { get; set; }

		public string access_token { get; set; }
		public string refresh_token { get; set; }
		public string refresh_token_expires_in { get; set; }
		public string scope { get; set; }
		public string token_type { get; set; }
        public string owner_id { get; set; }
        public string error { get; set; }
        public string error_description { get; set; }

        public string RingCentralId { get; set; }
        public string NameOnRingCentralAccount { get; set; }
        public string DisplayName { get; set; }
        public bool FirstTimeLogin { get; set; }
        public RingCentralContact Contact { get; set; }
        public class RingCentralContact
        {
            public string Firstname { get; set; }
            public string Lastname { get; set; }
            public string Company { get; set; }
            public string Email { get; set; }
            public string BusinessPhone { get; set; }
            public string Street { get; set; }
            public string City { get; set; }
            public string State { get; set; }
            public string Zip { get; set; }
            public string Country { get; set; }
        }
    }
    public class RingCentralAuthStateModel
    {
        public string RedirectUrl { get; set; }
        public string RandomHash { get; set; }
        public string RefId { get; set; }
        public string RefPlan { get; set; }
    }
}