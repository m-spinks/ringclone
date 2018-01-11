using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RingClone.Portal.Models
{
    public class ArchiveSearchModel
	{
        public string Type;
        public string DateFrom;
        public string DateTo;
        public string Extension;
        public string Name;
        public int PerPage;
        public int PageNumber;
        public int TotalRecords;
        public int TotalPages;
        public string NavTo;
        public List<ArchiveEntry> ArchiveEntries;
        public ArchiveNavigation Navigation;
        public class ArchiveEntry
		{
            public string Id;
            public ArchiveDisplay Display;
        }
        public class ArchiveDisplay
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

            //FILES
            public string Files;
            public string FilesTooltip;

            //OTHER
            public string RowClass;
        }
        public class ArchiveNavigation
        {
            public bool FirstPage;
            public bool PrevPage;
            public bool NextPage;
            public bool LastPage;
        }
    }
}