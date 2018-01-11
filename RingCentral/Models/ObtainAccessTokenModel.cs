using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RingCentral.Models
{
    public class ObtainAccessTokenModel
    {
        public string Username { get; set; }
        public string Extension { get; set; }
        public string Password { get; set; }
        public string AccessToken { get; set; }
        public string ExpiresIn { get; set; }
        public string TokenType { get; set; }
        public string RefreshToken { get; set; }
        public string RefreshTokenExpiresIn { get; set; }
        public string EndpointId { get; set; }
        public string OwnerId { get; set; }
        public string Scope { get; set; }
    }
}
