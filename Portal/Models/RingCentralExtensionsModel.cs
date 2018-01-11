using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RingClone.Portal.Models
{
    public class RingCentralExtensionsModel
    {
        public string nav;
        public Navigation navigation;
        public ICollection<Extension> extensions;
        public class Extension
        {
            public string Id;
            public string ExtensionNumber;
            public string Name;
            public string Firstname;
            public string Lastname;
            public string Email;
        }
        public class Navigation
        {
            public string firstPage;
            public string nextPage;
            public string prevPage;
            public string lastPage;
        }

    }
}