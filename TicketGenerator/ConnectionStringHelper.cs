using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TicketGenerator
{
	public class ConnectionStringHelper
	{
		public static string ConnectionString
		{
			get { return "Server=tcp:northtech.database.windows.net,1433;Database=RingClone;User ID=northtech@northtech;Password=123!@#qweQWE;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"; }
		}
	}
}
