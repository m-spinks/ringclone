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
    public class AuthChecker : AmazonAction
    {
        public bool IsAuthenticated;
        public string DisplayName;
        public AuthChecker(string username, int amazonAccountId) : base(username, amazonAccountId)
        {
        }

        public override void DoAction()
        {
            IsAuthenticated = false;
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
                    AmazonSecurityTokenServiceClient stsClient = new AmazonSecurityTokenServiceClient(AccessKeyId, SecretAccessKey, RegionEndpoint.GetBySystemName(region));
                    GetSessionTokenRequest getSessionTokenRequest = new GetSessionTokenRequest();
                    Credentials credentials = stsClient.GetSessionToken(getSessionTokenRequest).Credentials;
                    SessionAWSCredentials sessionCredentials =
                                              new SessionAWSCredentials(credentials.AccessKeyId,
                                                                        credentials.SecretAccessKey,
                                                                        credentials.SessionToken);
                    AmazonS3Client s3Client = new AmazonS3Client(sessionCredentials, RegionEndpoint.GetBySystemName(region));
                    var response = s3Client.ListBuckets();
                    IsAuthenticated = true;
                    DisplayName = response.Owner.DisplayName;
                    if (region != Region)
                    {
                        Region = region;
                        using (IDbConnection db = new SqlConnection(RingClone.AppConfig.Database.ConnectionString))
                        {
                            db.Execute("UPDATE T_AMAZONUSER SET Region=@region WHERE AmazonUserId=@amazonUserId", new { region = Region, amazonUserId = AmazonUserId });
                        }
                    }
                    break;
                }
                catch (Exception ex)
                {
                    if (!ex.Message.Contains("The bucket you are attempting to access must be addressed using the specified endpoint")
                        && !ex.Message.Contains("The security token included in the request is invalid")
                        && !ex.Message.Contains("STS is not activated in this region for account"))
                    {
                        //STS is not activated in this region for account:221870936645. Your account administrator can activate STS in this region using the IAM Console.
                        ResultException = ex;
                        break;
                    }
                }
            }
        }
    }
}
