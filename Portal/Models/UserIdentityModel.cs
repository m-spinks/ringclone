using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RingClone.Portal.Models
{
    public class UserIdentityModel
    {
        public string RingCentralId { get; set; }
        public string DisplayName { get; set; }
        public string Company { get; set; }
    }
}