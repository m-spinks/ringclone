using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RingClone.Portal.Models
{
    public class DownloadProcessorModel
	{
        public string Type { get; set; }
        public bool LogInd { get; set; }
        public bool ContentInd { get; set; }
        public string Filename { get; set; }
        public string DownloadId { get; set; }
        public List<string> Ids { get; set; }
    }
}