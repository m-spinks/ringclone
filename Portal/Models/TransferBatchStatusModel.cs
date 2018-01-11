using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RingClone.Portal.Models
{
    public class TransferBatchStatusModel
    {
        public int TransferBatchId;
        public string QueueMessage;
		public string ProcessStartMessage;
		public string ProcessStopMessage;
		public string StatusText;
		public string StatusIcon;
		public string StatusCss;
		public string CreateDate;
        public List<Ticket> Tickets;
        public class Ticket
        {
            public int TicketId;
            public string Title;
            public string StatusText;
			public string StatusIcon;
			public string StatusCss;
		}
    }
}