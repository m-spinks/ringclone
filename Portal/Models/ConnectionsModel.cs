using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RingClone.Portal.Models
{
	public class ConnectionsModel
	{
		public bool Active;
		public bool RingCentralCredentialsOk;
        public string DisplayName;
        public string Company;
        public string TransferSummary;
		public string LastTransfer;
		public string TimeZone;
		public bool SimpleAutomationExists;
		public bool SimpleAutomationIsActive;
		public bool SimpleAutomationPutInDatedSubfolder;
		public string SimpleAutomationDestination;
		public string SimpleAutomationFolder;
		public List<Destination> CompleteDestinations;
		public List<Destination> IncompleteDestinations;
		public class Destination
		{
			public string DestinationTitle;
			public string DestinationType;
			public int DestinationId;
		}

	}
}