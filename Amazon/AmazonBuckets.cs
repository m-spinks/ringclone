using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmazonActions
{
    public class AmazonBuckets : AmazonAction
    {
        public List<AmazonBucket> Buckets;
        public string Owner;
        public class AmazonBucket
        {
            public string BucketName { get; set; }
        }
        public AmazonBuckets(string username, int amazonAccountId) : base(username, amazonAccountId)
        {
        }

        public override void DoAction()
        {
            Buckets = new List<AmazonBucket>();
            var allRegions = new List<string>();
            if (!string.IsNullOrWhiteSpace(Region)) allRegions.Add(Region);
            foreach (var regionObject in RegionEndpoint.EnumerableAllRegions.Where(x => x.SystemName != Region))
            {
                allRegions.Add(regionObject.SystemName);
            }
            foreach (var region in allRegions)
            {
                try
                {
                    AmazonSecurityTokenServiceClient stsClient = new AmazonSecurityTokenServiceClient(AccessKeyId, SecretAccessKey, RegionEndpoint.USWest1);
                    GetSessionTokenRequest getSessionTokenRequest = new GetSessionTokenRequest();
                    Credentials credentials = stsClient.GetSessionToken(getSessionTokenRequest).Credentials;
                    SessionAWSCredentials sessionCredentials =
                                              new SessionAWSCredentials(credentials.AccessKeyId,
                                                                        credentials.SecretAccessKey,
                                                                        credentials.SessionToken);
                    AmazonS3Client s3Client = new AmazonS3Client(sessionCredentials, RegionEndpoint.GetBySystemName(region));
                    var response = s3Client.ListBuckets();
                    Owner = response.Owner.DisplayName;
                    if (string.IsNullOrWhiteSpace(Owner))
                    {
                        Owner = "root";
                    }
                    foreach (var bucket in response.Buckets)
                    {
                        var b = new AmazonBucket();
                        b.BucketName = bucket.BucketName;
                        Buckets.Add(b);
                    }
                    //if (region != Region)
                    //{
                    //    Region = region;
                    //    using (IDbConnection db = new SqlConnection(RingClone.AppConfig.Database.ConnectionString))
                    //    {
                    //        db.Execute("UPDATE T_AMAZONUSER SET Region=@region WHERE AmazonUserId=@amazonUserId", new { region = Region, amazonUserId = AmazonUserId });
                    //    }
                    //}
                    break;
                }
                catch (Exception ex)
                {
                    if (!ex.Message.Contains("The bucket you are attempting to access must be addressed using the specified endpoint")
                        && !ex.Message.Contains("The security token included in the request is invalid")
                        && !ex.Message.Contains("STS is not activated in this region for account"))
                    {
                        throw ex;
                    }
                }
            }
        }

        public class AmazonAccount
        {
            public int AmazonAccountId;
            public int AmazonUserId;
            public int AccountId;
            public string AmazonAccountName;
            public bool DeletedInd;
            public bool ActiveInd;
        }
        public class AmazonUser
        {
            public int AmazonUserId;
            public string Region;
            public string AccessKeyId;
            public string SecretAccessKey;
            public bool DeletedInd;
        }

    }
}
