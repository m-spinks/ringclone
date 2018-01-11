using FluentNHibernate.Mapping;
using AmazonActions;
using NHibernate;
using NHibernate.Criterion;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Transfer;
using Amazon.S3.Model;
using System.Data;
using System.Data.SqlClient;
using Dapper;

namespace AmazonActions
{
    public class AmazonDownload : AmazonAction
    {
        public string Email;
		private byte[] fileData;
        private string bucketName;
        private string key;

		public AmazonDownload(string username, int amazonAccountId)
            : base(username, amazonAccountId)
        {
			this.key = "";
        }
        public override void DoAction()
        {
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
                    TransferUtilityConfig config = new TransferUtilityConfig();
                    TransferUtility utility = new TransferUtility(s3Client, config);

                    var getRequest = new GetObjectRequest
                    {
                        BucketName = bucketName,
                        Key = key
                    };
                    using (GetObjectResponse response = s3Client.GetObject(getRequest))
                    {
                        if (response.HttpStatusCode != HttpStatusCode.OK)
                        {
                            ResultException = new Exception(response.HttpStatusCode.ToString());
                        }
                        else
                        {

                            using (Stream dataStream = response.ResponseStream)
                            using (MemoryStream ms = new MemoryStream())
                            {
                                int count = 0;
                                do
                                {
                                    byte[] buf = new byte[1024];
                                    count = dataStream.Read(buf, 0, 1024);
                                    ms.Write(buf, 0, count);
                                } while (dataStream.CanRead && count > 0);
                                fileData = ms.ToArray();
                            }
                        }
                    }
                    if (region != Region)
                    {
                        Region = region;
                        using (IDbConnection db = new SqlConnection(RingClone.AppConfig.Database.ConnectionString))
                        {
                            db.Execute("UPDATE T_AMAZONUSER SET Region=@region WHERE AmazonUserId=@amazonUserId", new { region = Region, amazonUserId = AmazonUserId });
                        }
                    }
                    break;
                    //if (!string.IsNullOrWhiteSpace(navigateToOrCreateSubFolder))
                    //    utility.Upload(stream, bucketName, key + navigateToOrCreateSubFolder);
                    //else
                    //    utility.Upload(stream, bucketName, key);
                }
                //catch (AmazonS3Exception amazonS3Exception)
                //{
                //    //FOR REFERENCE LATER
                //    if (amazonS3Exception.ErrorCode != null &&
                //        (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId")
                //        ||
                //        amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
                //    {
                //        throw new Exception("Check the provided AWS Credentials.");
                //    }
                //    else
                //    {
                //        throw new Exception("Error occurred: " + amazonS3Exception.Message);
                //    }
                //    ResultException = amazonS3Exception;
                //}
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

        public AmazonDownload BucketName(string bucketName)
        {
            this.bucketName = bucketName;
            return this;
        }
        public AmazonDownload Key(string key)
        {
            this.key = key;
            return this;
        }
		public byte[] FileData()
		{
			return this.fileData;
		}
    }
}
