using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArchiveIndexer
{
	public class ConnectionStringHelper
	{
        public static string ConnectionString
        {
            get { return "Server=tcp:northtech.database.windows.net,1433;Database=RingClone;User ID=northtech@northtech;Password=123!@#qweQWE;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"; }
        }
        public static string StorageConnectionString
        {
            get { return "DefaultEndpointsProtocol=https;AccountName=ringclone;AccountKey=isulu48ionaeU2aYu2mj2P6HvuF/6cFvvIVHZLvRgsoFFK5yp+2qoxAeUi/P5hOVu2+uSvRRpA52k79NeEG6sw=="; }
        }
    }
}
