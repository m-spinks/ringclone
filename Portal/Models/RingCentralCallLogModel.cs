using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RingClone.Portal.Models
{
    public class RingCentralCallLogModel
	{
        public DateTime? DateFrom;
        public DateTime? DateTo;
        public List<Call> Calls;
        public List<string> Extensions;
        [JsonIgnore]
        public IEnumerable<TicketLog> TicketLogs;
        [JsonIgnore]
        public IEnumerable<TransferRule> TransferRules;
        public CallLogNavigation Navigation;
        public class Call
		{
            public string Id;
			public DateTime Time;
			public string TimeLabel;
			public string TimeSince;
			public string FromNumber;
			public string FromName;
			public string FromLocation;
			public string ToNumber;
			public string ToName;
			public string ToLocation;
			public string Numbers;
			public string Direction;
            public string Action;
            public string Result;
            public string Length;
            public string RecommendedFileName;
			public string SerializedPacket;
            public bool Transferred;
            public string TransferredOn;
            public string ContentUri;
            public CallDisplay Display;
        }
        public class CallDisplay
        {
            public string Type;
            public string PhoneNumber;
            public string Name;
            public string Date;
            public string Action;
            public string Result;
            public string Length;
            public string ArchiveStatus;
            public string ArchiveTooltip;
            public string ArchiveIcon;
            public string RowClass;
        }
        public class TicketLog
        {
            public string CallId;
            public DateTime? TicketLogStopDate;
            public bool ErrorInd;
            public bool DeletedInd;
        }
        public class CallLogNavigation
        {
            public string FirstPage;
            public string PrevPage;
            public string NextPage;
            public string LastPage;
        }
        public class TransferRule
        {
            public int TransferRuleId;
            public int AccountId;
            public bool DeletedInd;
            public bool ActiveInd;
            public string Frequency;
            public string TimeOfDay;
        }
    }
}