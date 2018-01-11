using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RingClone.Portal.Models
{
    public class DownloadPollingModel
	{
        public int TotalNotDownloaded;
        public int TotalNotSeen;
        public List<DownloadPollingItem> Downloads { get; set; }
        public class DownloadPollingItem
        {
            public string DownloadId;
            public DateTime CreateDate;
            public string Filename;
            public string Tooltip;
            public int Percent;
            public bool ErrorInd;
            public bool DownloadedInd;
            public bool CompleteInd;
        }
    }
}