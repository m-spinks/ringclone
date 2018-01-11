using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using static TicketProcessor.Functions;

namespace TicketProcessor
{
	// To learn more about Microsoft Azure WebJobs SDK, please see http://go.microsoft.com/fwlink/?LinkID=320976
	class Program
	{
		// Please set the following connection strings in app.config for this WebJob to run:
		// AzureWebJobsDashboard and AzureWebJobsStorage
		static void Main()
		{
#if DEBUG
            var textWriter = Console.Out;
            Functions.ExecuteBatch(6700, textWriter);
#else
            var host = new JobHost();
            // The following code ensures that the WebJob will be running continuously
            host.RunAndBlock();
#endif
            //while (true)
            //{
            //	Functions.Execute();
            //	System.Threading.Thread.Sleep(3000);
            //}

        }
    }
}
