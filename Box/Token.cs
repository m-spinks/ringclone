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

namespace Box
{
	public class Token
	{

		public void RefreshAccessToken(ref string accessToken, ref string refreshToken, ref string expiresIn, ref string tokenType)
		{
            try
            {
                var boxRefreshTokenUrl = "https://app.box.com/api/oauth2/token/";
                var requestParams = new NameValueCollection();
                requestParams.Add("grant_type", "refresh_token");
                requestParams.Add("refresh_token", refreshToken);
                requestParams.Add("client_id", RingClone.AppConfig.Box.ClientId);
                requestParams.Add("client_secret", RingClone.AppConfig.Box.ClientSecret);
                var array = (from key in requestParams.AllKeys
                             from value in requestParams.GetValues(key)
                             select string.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(value)))
                    .ToArray();
                var queryString = string.Join("&", array);
                var postData = queryString;
                var url = boxRefreshTokenUrl;
                WebRequest request = WebRequest.Create(url);
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                //request.Headers.Add("Authorization", "Bearer " + accessToken);
                byte[] byteArray = Encoding.UTF8.GetBytes(queryString);
                Stream dataStream = request.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                WebResponse response = request.GetResponse();
                dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string responseFromServer = reader.ReadToEnd();
                JavaScriptSerializer jss = new JavaScriptSerializer();
                var responseModel = jss.Deserialize<BoxTokenResponse>(responseFromServer);
                reader.Close();
                dataStream.Close();
                response.Close();

                accessToken = responseModel.access_token;
                refreshToken = responseModel.refresh_token;
                expiresIn = responseModel.expires_in;
                tokenType = responseModel.token_type;
            }
            catch (WebException ex)
            {
                Stream dataStream = ex.Response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string responseFromServer = reader.ReadToEnd();
                var err = ex.Message;
                throw new WebException(err);
            }
		}
		private class BoxTokenResponse
		{
			public string access_token { get; set; }
			public string expires_in { get; set; }
			public string token_type { get; set; }
			public string refresh_token { get; set; }
			public string error { get; set; }
			public string error_description { get; set; }
		}


	}
}
