using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmazonActions
{
    public class AmazonAccountInfo : AmazonAction
    {
        public string DisplayName;
        public AmazonAccountInfo(string username, int amazonAccountId) : base(username, amazonAccountId)
        {
        }

        public override void DoAction()
        {
            string region = RegionEndpoint.USWest2.SystemName;
            AmazonSecurityTokenServiceClient stsClient = new AmazonSecurityTokenServiceClient(AccessKeyId, SecretAccessKey, RegionEndpoint.GetBySystemName(region));
            GetSessionTokenRequest getSessionTokenRequest = new GetSessionTokenRequest();
            Credentials credentials = stsClient.GetSessionToken(getSessionTokenRequest).Credentials;
            SessionAWSCredentials sessionCredentials =
                                      new SessionAWSCredentials(credentials.AccessKeyId,
                                                                credentials.SecretAccessKey,
                                                                credentials.SessionToken);
            AmazonS3Client s3Client = new AmazonS3Client(sessionCredentials, RegionEndpoint.GetBySystemName(region));
            var response = s3Client.ListBuckets();
            DisplayName = response.Owner.DisplayName;
        }
    }
}
