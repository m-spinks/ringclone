using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RingClone.AppConfig
{
	public class Database
	{
		public static string ConnectionString
		{
            get { return ConfigurationManager.ConnectionStrings["RingCloneDatabase"].ConnectionString; }
		}
	}
}
