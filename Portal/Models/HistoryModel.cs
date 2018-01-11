using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RingClone.Portal.Models
{
	public class HistoryModel
	{
        public int TotalBatches;
		public List<TransferBatch> TransferBatches;
		public class TransferBatch
		{
			public int TransferBatchId;
            public string Title;
            public string LogNumber;
            public int TotalTickets;
            public DateTime CreateDate;
			public DateTime? StartDate;
			public string StatusText;
			public string StatusCss;
		}
	}
}