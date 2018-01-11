using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RingClone.Portal.Models
{
	public class SimpleAutomationModel
	{
        public int TransferRuleId { get; set; }
		public bool ActiveInd { get; set; }
		public string Destination { get; set; }
		public int DestinationBoxAccountId { get; set; }
		public int DestinationBoxTokenId { get; set; }
		public int DestinationGoogleAccountId { get; set; }
		public int DestinationGoogleTokenId { get; set; }
		public int DestinationFtpAccountId { get; set; }
		public string DestinationFolderId { get; set; }
		public string DestinationFolderPath { get; set; }
		public string DestinationFolderName { get; set; }
		public string DestinationFolderLabel { get; set; }
        public string DestinationBucketName { get; set; }
        public string DestinationPrefix { get; set; }
        public bool PutInDatedSubFolder { get; set; }
		public string BoxUrl { get; set; }
		public string BoxEmail { get; set; }
		public string GoogleUrl { get; set; }
		public string GoogleEmail { get; set; }
		public string AccessToken { get; set; }
		public string RefreshToken { get; set; }
        public int AmazonAccountId { get; set; }
        public string AmazonAccessKeyId { get; set; }
        public string AmazonSecretAccessKey { get; set; }
        public string AmazonDisplayName { get; set; }
        public string Frequency { get; set; }
        public string TimeOfDay { get; set; }
        public bool VoiceLogInd { get; set; }
        public bool VoiceContentInd { get; set; }
        public bool FaxLogInd { get; set; }
        public bool FaxContentInd { get; set; }
        public bool SmsLogInd { get; set; }
        public bool SmsContentInd { get; set; }

        //BILLING (IF UPGRADE IS REQUIRED
        public string BillingEmail { get; set; }
		public string PlanId { get; set; }

	}
}