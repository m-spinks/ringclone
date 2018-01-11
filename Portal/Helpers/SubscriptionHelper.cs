using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RingClone.Portal.Helpers
{
    public class SubscriptionHelper
    {
        public static List<SubcriptionPlan> SubscriptionPlans = new List<SubcriptionPlan>()
        {
            new SubcriptionPlan()
            {
                Id = "ringclone_bronze",
                Cost = 0,
                MarketingTitle = "Bronze Plan",
                MarketingLabel = "Subscription for Bronze Plan",
                MarketingDetails = "<ul><li>Up to 100 archives a month</li><li>Archive to your Google Drive account</li><li>Archive to your Box.com account</li><li>Archive to your Amazon account</li><li>Access recordings from ALL extensions</li><li>Detailed logging of transferred files</li><li>Mobile app access</li><li>Limited tech support</li></ul>",
                MaxTransfersPerMonth = 100
            },
            new SubcriptionPlan()
            {
                Id = "ringclone_silver",
                Cost = 9,
                MarketingTitle = "Silver Plan",
                MarketingLabel = "Subscription for Silver Plan",
                MarketingDetails = "<ul><li><b>Autotmatic backups</b></li><li>Up to 500 archives a month</li><li>Configurable archiving (logs, voice, fax, sms/text)</li><li>Archive to your Google Drive account</li><li>Archive to your Box.com account</li><li>Archive to your Amazon account</li><li>Access recordings from ALL extensions</li><li>Detailed logging of transferred files</li><li>Mobile app access</li><li>Full tech support</li></ul>",
                MaxTransfersPerMonth = 500
            },
            new SubcriptionPlan()
            {
                Id = "ringclone_gold",
                Cost = 19,
                MarketingTitle = "Gold Plan",
                MarketingLabel = "Subscription for Gold Plan",
                MarketingDetails = "<ul><li><b>Autotmatic backups</b></li><li>Up to 1500 archives a month</li><li>Configurable archiving (logs, voice, fax, sms/text)</li><li>Archive to your Google Drive account</li><li>Archive to your Box.com account</li><li>Archive to your Amazon account</li><li>Access recordings from ALL extensions</li><li>Detailed logging of transferred files</li><li>Mobile app access</li><li>Full tech support</li></ul>",
                MaxTransfersPerMonth = 1500
            },
            new SubcriptionPlan()
            {
                Id = "ringclone_platinum",
                Cost = 39,
                MarketingTitle = "Platinum Plan",
                MarketingLabel = "Subscription for Platinum Plan",
                MarketingDetails = "<ul><li><b>Autotmatic backups</b></li><li><b>Unlimited archives every month</b></li><li>Configurable archiving (logs, voice, fax, sms/text)</li><li>Archive to your Google Drive account</li><li>Archive to your Box.com account</li><li>Archive to your Amazon account</li><li>Access recordings from ALL extensions</li><li>Detailed logging of transferred files</li><li>Mobile app access</li><li>Full tech support</li></ul>",
                MaxTransfersPerMonth = 999999
            }
        };
        public class SubcriptionPlan
        {
            public string Id;
            public decimal Cost;
            public int MaxTransfersPerMonth;
            public string MarketingTitle;
            public string MarketingLabel;
            public string MarketingDetails;
        }
    }
}