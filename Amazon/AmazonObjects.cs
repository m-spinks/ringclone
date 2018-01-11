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
    public class AmazonObjects : AmazonAction
    {
        public List<AmazonObject> Objects;
        public List<string> CommonPrefixes;
        private string bucketName;
        private string prefix;
        private string delimiter;
        public string BucketName
        {
            get
            {
                return bucketName;
            }
        }
        public string Prefix
        {
            get
            {
                return prefix;
            }
        }
        public string Delimiter
        {
            get
            {
                return delimiter;
            }
        }
        public class AmazonObject
        {
            public string Key { get; set; }
        }
        public AmazonObjects(string username, int amazonAccountId, string bucketName, string prefix, string delimiter) : base(username, amazonAccountId)
        {
            this.bucketName = bucketName;
            this.prefix = prefix;
            this.delimiter = delimiter;
        }

        public override void DoAction()
        {
            Objects = new List<AmazonObject>();
            CommonPrefixes = new List<string>();
            var allRegions = new List<string>();
            var accessed = false;
            if (!string.IsNullOrWhiteSpace(Region)) allRegions.Add(Region);
            foreach (var regionObject in RegionEndpoint.EnumerableAllRegions.Where(x => x.SystemName != Region))
            {
                allRegions.Add(regionObject.SystemName);
            }
            foreach (var region in allRegions)
            {
                try
                {
                    AmazonSecurityTokenServiceClient stsClient = new AmazonSecurityTokenServiceClient(AccessKeyId, SecretAccessKey, RegionEndpoint.GetBySystemName(region));
                    GetSessionTokenRequest getSessionTokenRequest = new GetSessionTokenRequest();
                    Credentials credentials = stsClient.GetSessionToken(getSessionTokenRequest).Credentials;
                    SessionAWSCredentials sessionCredentials =
                                              new SessionAWSCredentials(credentials.AccessKeyId,
                                                                        credentials.SecretAccessKey,
                                                                        credentials.SessionToken);
                    AmazonS3Client s3Client = new AmazonS3Client(sessionCredentials, RegionEndpoint.GetBySystemName(region));
                    var options = new Amazon.S3.Model.ListObjectsRequest();
                    if (!string.IsNullOrWhiteSpace(bucketName))
                        options.BucketName = bucketName;
                    if (!string.IsNullOrWhiteSpace(prefix))
                        options.Prefix = prefix;
                    if (!string.IsNullOrWhiteSpace(delimiter))
                        options.Delimiter = delimiter;
                    var response = s3Client.ListObjects(options);
                    foreach (var obj in response.S3Objects)
                    {
                        var ao = new AmazonObject();
                        ao.Key = obj.Key;
                        Objects.Add(ao);
                    }
                    foreach (var obj in response.CommonPrefixes)
                    {
                        CommonPrefixes.Add(obj);
                    }
                    if (region != Region)
                    {
                        Region = region;
                        using (IDbConnection db = new SqlConnection(RingClone.AppConfig.Database.ConnectionString))
                        {
                            db.Execute("UPDATE T_AMAZONUSER SET Region=@region WHERE AmazonUserId=@amazonUserId", new { region = Region, amazonUserId = AmazonUserId });
                        }
                    }
                    accessed = true;
                    break;
                }
                catch (Exception ex)
                {
                    if (!ex.Message.Contains("The bucket you are attempting to access must be addressed using the specified endpoint")
                        && !ex.Message.Contains("The security token included in the request is invalid")
                        && !ex.Message.Contains("STS is not activated in this region for account"))
                    {
                        //STS is not activated in this region for account:221870936645. Your account administrator can activate STS in this region using the IAM Console.
                        throw ex;
                    }
                }
            }
            if (!accessed)
                ResultException = new Exception("You do not have access to this resource");
        }
    }
}
