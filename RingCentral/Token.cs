using NHibernate;
using NHibernate.Criterion;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Web;

namespace RingCentral
{
	public class Token
	{

        public RingCentralTokenResponse RefreshAccessToken(string refreshToken)
		{
            var requestParams = new NameValueCollection();
            requestParams.Add("grant_type", "refresh_token");
            requestParams.Add("refresh_token", refreshToken);
            var array = (from key in requestParams.AllKeys
                         from value in requestParams.GetValues(key)
                         select string.Format("{0}={1}", key, HttpUtility.UrlEncode(value)))
                .ToArray();
            var postString = string.Join("&", array);
            byte[] byteArray = Encoding.UTF8.GetBytes(postString);
            var url = RingCentral.Config.TokenUri;
            WebRequest request = WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded;charset=UTF-8";
            request.Headers.Add("Authorization", "Basic " + RingCentral.Config.Base64KeySecret);
            request.ContentLength = postString.Length;
            StreamWriter requestWriter = new StreamWriter(request.GetRequestStream());
            requestWriter.Write(postString);
            requestWriter.Close();
            try
            {
                WebResponse response = request.GetResponse();
                Stream dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string responseFromServer = reader.ReadToEnd();
                JavaScriptSerializer jss = new JavaScriptSerializer();
                var responseModel = jss.Deserialize<RingCentralTokenResponse>(responseFromServer);
                return responseModel;
            }
            catch (WebException wex)
            {
                if (wex.Response != null)
                {
                    using (var errorResponse = (HttpWebResponse)wex.Response)
                    {
                        using (var reader = new StreamReader(errorResponse.GetResponseStream()))
                        {
                            string error = reader.ReadToEnd();
                            var str = error;
                            //TODO: use JSON.net to parse this string and look at the error message
                        }
                    }
                }
                throw wex;
            }


            //var ringCentralRefreshTokenUrl = "https://platform.ringcentral.com/restapi/oauth/token";
            //var requestParams = new NameValueCollection();
            //requestParams.Add("grant_type", "refresh_token");
            //requestParams.Add("refresh_token", refreshToken);
            //var array = (from key in requestParams.AllKeys
            //                from value in requestParams.GetValues(key)
            //                select string.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(value)))
            //    .ToArray();
            //var postString = string.Join("&", array);
            //byte[] byteArray = Encoding.UTF8.GetBytes(postString);
            //var url = ringCentralRefreshTokenUrl;
            //WebRequest request = WebRequest.Create(ringCentralRefreshTokenUrl);
            //request.Method = "POST";
            //request.ContentType = "application/x-www-form-urlencoded;charset=UTF-8";
            //request.Headers.Add("Authorization", "Basic " + accessToken);
            //request.ContentLength = byteArray.Length;
            //Stream writer = request.GetRequestStream();
            //writer.Write(byteArray, 0, byteArray.Length);
            //writer.Close();
            //WebResponse response = request.GetResponse();
            //Stream dataStream = response.GetResponseStream();
            //StreamReader reader = new StreamReader(dataStream);
            //string responseFromServer = reader.ReadToEnd();
            //JavaScriptSerializer jss = new JavaScriptSerializer();
            //var responseModel = jss.Deserialize<RingCentralTokenResponse>(responseFromServer);
            //reader.Close();
            //dataStream.Close();
            //response.Close();


		}
		public class RingCentralTokenResponse
		{
            public string access_token { get; set; }
            public string token_type { get; set; }
            public string expires_in { get; set; }
            public string refresh_token { get; set; }
            public string refresh_token_expires_in { get; set; }
            public string scope { get; set; }
            public string owner_id { get; set; }
            public string endpoint_id { get; set; }
        }


    }
}
