using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RingClone.Portal.Models
{
	public class AdHocTransferModel
	{
        public string SubscriptionPlan { get; set; }
        public int AvailableTransfersLeft { get; set; }
		public string Source { get; set; }
		public string SourceLabel {get; set;}
        public DestinationType Destination {get; set;}
        public string DestinationFolderId {get; set;}
        public string DestinationFolderName {get; set;}
        public string DestinationFolderPath { get; set; }
        public string DestinationBucketName { get; set; }
        public string DestinationPrefix { get; set; }
        public string BoxUrl { get; set; }
        public int BoxAccountId { get; set; }
        public int BoxTokenId { get; set; }
        public string BoxEmail { get; set; }
        public string GoogleUrl { get; set; }
        public int GoogleAccountId { get; set; }
        public int GoogleTokenId { get; set; }
        public string GoogleEmail { get; set; }
        public string AccessToken { get; set; }
		public string RefreshToken {get; set;}
        public int AmazonAccountId { get; set; }
        public string AmazonAccessKeyId { get; set; }
        public string AmazonSecretAccessKey { get; set; }
        public string AmazonDisplayName { get; set; }
        public int TransferBatchId { get; set; }
        public string Extension { get; set; }
        public string Type { get; set; }
        public string ArchiveDescription { get; set; }
        public ICollection<string> LogEntries { get; set; }
        public ICollection<LogEntryData> LogEntriesWithData { get; set; }
        public class LogEntryData
		{
            public string Id;
            public string RawData;
            public string SaveAsFilename;
        }
        public enum DestinationType
        {
            Box,
            Google,
            Amazon,
            Ftp
        }
	}
}