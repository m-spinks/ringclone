using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RingCentral.Models
{
    public class EnsureRingCentralAuthModel
    {
        public int RingCentralTokenId { get; set; }
        public string TokenType { get; set; }
        public string AccessToken { get; set; }
        public string ExpiresIn { get; set; }
        public string RefreshToken { get; set; }
        public string RefreshTokenExpiresIn { get; set; }
        public DateTime LastRefreshedOn { get; set; }
        public bool DeletedInd { get; set; }
        public bool Changed { get; set; }
    }
}
