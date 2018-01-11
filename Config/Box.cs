using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RingClone.Config
{
	public static class Box
	{
		public static string ClientId
		{
			get
			{
				return "qf37x708g1ejew3s02ust23frdo68yeh";
			}
		}
		public static string ClientSecret
		{
			get
			{
				return "XhhdEbIQ2DiQmjp0l3RdC7I505UjBkKN";
			}
		}
		public static string AuthUri
		{
			get
			{
				return "https://app.box.com/api/oauth2/authorize";
			}
		}
		public static string TokenUri
		{
			get
			{
				return "https://app.box.com/api/oauth2/token";
			}
		}
		public static string RedirectUri
		{
			get
			{
				return "http://localhost:17212/boxauthenticated";
			}
		}
	}
}
