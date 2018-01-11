using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.Script.Serialization;

namespace RingClone.Config
{
    public static class Google
    {
		public static string ClientId
		{
			get
			{
				var assembly = Assembly.GetExecutingAssembly();
				var resourceName = "RingClone.Config.data.client_secret.google.json";
				using (Stream stream = assembly.GetManifestResourceStream(resourceName))
				{
					StreamReader reader = new StreamReader(stream);
					string fileContentString = reader.ReadToEnd();
					JavaScriptSerializer jss = new JavaScriptSerializer();
					var responseModel = jss.Deserialize<ClientSecretFile>(fileContentString);
					if (responseModel != null && !string.IsNullOrEmpty(responseModel.web.client_id))
						return responseModel.web.client_id;
					return "";
				}
			}
		}
		public static string ClientSecret
		{
			get
			{
				var assembly = Assembly.GetExecutingAssembly();
				var resourceName = "RingClone.Config.data.client_secret.google.json";
				using (Stream stream = assembly.GetManifestResourceStream(resourceName))
				{
					StreamReader reader = new StreamReader(stream);
					string fileContentString = reader.ReadToEnd();
					JavaScriptSerializer jss = new JavaScriptSerializer();
					var responseModel = jss.Deserialize<ClientSecretFile>(fileContentString);
					if (responseModel != null && !string.IsNullOrEmpty(responseModel.web.client_secret))
						return responseModel.web.client_secret;
					return "";
				}
			}
		}
		public static string TokenUri
		{
			get
			{
				var assembly = Assembly.GetExecutingAssembly();
				var resourceName = "RingClone.Config.data.client_secret.google.json";
				using (Stream stream = assembly.GetManifestResourceStream(resourceName))
				{
					StreamReader reader = new StreamReader(stream);
					string fileContentString = reader.ReadToEnd();
					JavaScriptSerializer jss = new JavaScriptSerializer();
					var responseModel = jss.Deserialize<ClientSecretFile>(fileContentString);
					if (responseModel != null && !string.IsNullOrEmpty(responseModel.web.token_uri))
						return responseModel.web.token_uri;
					return "";
				}
			}
		}
		public static string AuthUri
		{
			get
			{
				var assembly = Assembly.GetExecutingAssembly();
				var resourceName = "RingClone.Config.data.client_secret.google.json";
				using (Stream stream = assembly.GetManifestResourceStream(resourceName))
				{
					StreamReader reader = new StreamReader(stream);
					string fileContentString = reader.ReadToEnd();
					JavaScriptSerializer jss = new JavaScriptSerializer();
					var responseModel = jss.Deserialize<ClientSecretFile>(fileContentString);
					if (responseModel != null && !string.IsNullOrEmpty(responseModel.web.auth_uri))
						return responseModel.web.auth_uri;
					return "";
				}
			}
		}
		public static string RedirectUri
		{
			get
			{
				return "http://app.ringclone.com/googleauthenticated";
			}
		}
		private class ClientSecretFile
		{
			public Web web;
			public class Web
			{
				public string client_id { get; set; }
				public string project_id { get; set; }
				public string auth_uri { get; set; }
				public string token_uri { get; set; }
				public string client_secret { get; set; }
			}
		}
    }
}
