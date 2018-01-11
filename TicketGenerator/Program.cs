using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using static TicketGenerator.Functions;

namespace TicketGenerator
{
	// To learn more about Microsoft Azure WebJobs SDK, please see http://go.microsoft.com/fwlink/?LinkID=320976
	class Program
	{
		// Please set the following connection strings in app.config for this WebJob to run:
		// AzureWebJobsDashboard and AzureWebJobsStorage
		static void Main()
		{
			//var host = new JobHost();
			//// The following code ensures that the WebJob will be running continuously
			//host.RunAndBlock();

			while (true)
			{
                Console.WriteLine("Looking for rules to be run this hour...");
				var model = new TicketGeneratorModel();
				model.Now = System.DateTime.Now.ToUniversalTime();
				getBatchesToAdd(model);
				getAccountInfoForBatches(model);

				getTickets(model);
				saveBatchesAndTickets(model);

				System.Threading.Thread.Sleep(7000);
			}

		}

	}

}
