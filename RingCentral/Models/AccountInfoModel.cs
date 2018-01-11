using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RingCentral.Models
{
    public class AccountInfoModel
    {
        public string id { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public string status { get; set; }
        public Contact contact { get; set; }
        public class Contact
        {
            public string firstName { get; set; }
            public string lastName { get; set; }
            public string company { get; set; }
            public string email { get; set; }
        }
    }
}
