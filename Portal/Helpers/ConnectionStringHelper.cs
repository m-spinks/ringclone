using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace RingClone.Portal.Helpers
{
	public class ConnectionStringHelper
	{
		public static string ConnectionString
		{
            get { return ConfigurationManager.ConnectionStrings["RingCloneDatabase"].ConnectionString; }
        }
        public static string StorageConnectionString
        {
            get { return ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"].ConnectionString; }
        }
    }
}