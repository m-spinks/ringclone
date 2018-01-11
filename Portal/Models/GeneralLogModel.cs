using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RingClone.Portal.Models
{
    public class GeneralLogModel
	{
        public string Type;
        public string DateFrom;
        public string DateTo;
        public string Extension;
        public int PerPage;
        public string NavTo;
        public List<LogEntry> LogEntries;
        public LogNavigation Navigation;
        [JsonIgnore]
        public IEnumerable<Ticket> Tickets;
        [JsonIgnore]
        public IEnumerable<TransferRule> TransferRules;
        public class LogEntry
		{
            public string Id;
            public LogDisplay Display;
        }
        public class LogDisplay
        {
            public string Type;
            public string PhoneNumber;
            public string Name;
            public string Date;

            //VOICE
            public string Action;
            public string Result;
            public string Length;

            //FAXES
            public string MessageStatus;
            public string CoverPageText;
            public string FaxPageCount;
            public string Direction;

            //SMS
            public string Subject;

            public string ArchiveStatus;
            public string ArchiveTooltip;
            public string ArchiveIcon;
            public string RowClass;
        }
        public class Ticket
        {
            public string CallId;
            public string MessageId;
            public DateTime? CompleteDate;
            public bool ErrorInd;
            public bool DeletedInd;
        }
        public class LogNavigation
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
            public bool VoiceLogInd;
            public bool VoiceContentInd;
            public bool FaxLogInd;
            public bool FaxContentInd;
            public bool SmsLogInd;
            public bool SmsContentInd;
            public bool DeletedInd;
            public bool ActiveInd;
            public string Frequency;
            public string TimeOfDay;
        }
    }
}