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
    public class Helpers
    {
        public class ValidateModel
        {
            public bool Validated;
            public string DisplayName;
        }
        public static ValidateModel Validate(string AccessKeyId, string SecrectAccessKey)
        {
            var model = new ValidateModel();
            model.Validated = false;
            try
            {
                string region = Amazon.RegionEndpoint.USWest2.SystemName;
                // In real applications, the following code is part of your trusted code. It has 
                // your security credentials you use to obtain temporary security credentials.
                AmazonSecurityTokenServiceClient stsClient = new AmazonSecurityTokenServiceClient(AccessKeyId, SecrectAccessKey, Amazon.RegionEndpoint.GetBySystemName(region));
                GetSessionTokenRequest getSessionTokenRequest = new GetSessionTokenRequest();
                // Following duration can be set only if temporary credentials are requested by an IAM user.
                getSessionTokenRequest.DurationSeconds = 7200; // seconds.
                Credentials credentials = stsClient.GetSessionToken(getSessionTokenRequest).Credentials;
                SessionAWSCredentials sessionCredentials =
                                          new SessionAWSCredentials(credentials.AccessKeyId,
                                                                    credentials.SecretAccessKey,
                                                                    credentials.SessionToken);

                // The following will be part of your less trusted code. You provide temporary security
                // credentials so it can send authenticated requests to Amazon S3. 
                // Create Amazon S3 client by passing in the basicSessionCredentials object.
                AmazonS3Client s3Client = new AmazonS3Client(sessionCredentials, Amazon.RegionEndpoint.GetBySystemName(region));

                // Test. For example, send request to list object key in a bucket.
                var response = s3Client.ListBuckets();
                model.Validated = true;
                model.DisplayName = response.Owner.DisplayName;
            }
            catch (AmazonS3Exception ex)
            {
                
            }
            catch (Exception ex)
            {

            }
            return model;
        }
    }
}
