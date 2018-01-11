using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RingClone.Portal.Helpers
{
    public static class EnvironmentHelper
    {
        public static bool Debug()
        {
#if DEBUG
            return true;
#else
            return false;
#endif
        }
    }
}