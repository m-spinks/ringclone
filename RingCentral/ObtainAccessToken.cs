using RingCentral.Models;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;

namespace RingCentral
{
    public class ObtainAccessToken
    {
        public void Execute(ObtainAccessTokenModel model)
        {
            if (model == null)
                return;
            try
            {
                var requestParams = new NameValueCollection();
                requestParams.Add("grant_type", "password");
                requestParams.Add("username", model.Username);
				if (!string.IsNullOrEmpty(model.Extension))
					requestParams.Add("extension", model.Extension);
                requestParams.Add("password", model.Password);
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
                WebResponse response = request.GetResponse();
                Stream dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string responseFromServer = reader.ReadToEnd();
                JavaScriptSerializer jss = new JavaScriptSerializer();
                var data = jss.Deserialize<RingCentralTokenData>(responseFromServer);
                if (data != null)
                {
                    model.AccessToken = data.access_token;
                    model.ExpiresIn = data.expires_in;
                    model.RefreshToken = data.refresh_token;
                    model.TokenType = data.token_type;
                    model.RefreshTokenExpiresIn = data.refresh_token_expires_in;
                    model.Scope = data.scope;
                    model.EndpointId = data.endpoint_id;
                    model.OwnerId = data.owner_id;
                }
                reader.Close();
                dataStream.Close();
                response.Close();
            }
			catch (WebException ex)
			{
				var httpResponse = (HttpWebResponse)ex.Response;
				string err;
				using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
				{
					err = streamReader.ReadToEnd();
				}
				var i = 0;
				i++;
			}
			catch (Exception ex)
            {
            }
        }
        private class RingCentralTokenData
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
