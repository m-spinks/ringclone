using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using System.Data;
using System.Data.SqlClient;
using Dapper;
using System.Configuration;

namespace ConfigMonitor
{
    public class Functions
    {
        // This function will get triggered/executed when a new message is written 
        // on an Azure Queue called queue.
        public static void ProcessQueueMessage([QueueTrigger("queue")] string message, TextWriter log)
        {
            log.WriteLine(message);
        }

        public static void CheckConfigs()
        {
            var foundConnString = false;
            var foundAppSetting = false;

            try
            {
                var c = ConfigurationManager.ConnectionStrings["RingCloneDatabase"].ConnectionString;
                if (!string.IsNullOrWhiteSpace(c))
                {
                    foundConnString = true;
                }
            }
            catch (Exception ex)
            {

            }
            try
            {
                string a = ConfigurationManager.AppSettings["RingCentral_AppKey"];
                if (!string.IsNullOrWhiteSpace(a))
                {
                    foundAppSetting = true;
                }
            }
            catch (Exception ex)
            {

            }

            var connStringForLogging = "Server=tcp:northtech.database.windows.net,1433;Database=RingClone;User ID=northtech@northtech;Password=123!@#qweQWE;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
            using (IDbConnection db = new SqlConnection(connStringForLogging))
            {
                db.Execute("INSERT INTO T_CONFIGMONITOR (MonitorDate, ConnectionStringInd, AppSettingInd) VALUES (@monitorDate, @connectionStringInd, @appSettingInd)", new { monitorDate = DateTime.Now.ToUniversalTime(), connectionStringInd = foundConnString, appSettingInd = foundAppSetting });
            }
        }
    }
}
