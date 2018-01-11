using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RingCentral
{
    public static class Config
    {
		public static string AppKey
		{
			get
			{
#if DEBUG
                return ConfigurationManager.AppSettings["RingCentral_AppKey"];
                return "loSZsvRBTSmz1GquL_-TVA"; // DEV SANDBOX
#else
                return ConfigurationManager.AppSettings["RingCentral_AppKey"];
                return "W0eMuFqyR3eT_dy82kgjoQ";
#endif
            }
        }
		public static string AppSecret
		{
			get
			{
#if DEBUG
                return ConfigurationManager.AppSettings["RingCentral_AppSecret"];
                return "oTz0vr0ARWOsoSZGJmknOgFmAG7J-YSH6s2CpCWLx_HQ"; // DEV SANDBOX
#else
                return ConfigurationManager.AppSettings["RingCentral_AppSecret"];
                return "wzF1qSRIQkK5r3KT7uErfwUcFCuhEWQAyA0D4LwJaiKw";
#endif
            }
        }
		public static string Base64KeySecret
		{
			get
			{
#if DEBUG
                return ConfigurationManager.AppSettings["RingCentral_Base64KeySecret"];
                return "bG9TWnN2UkJUU216MUdxdUxfLVRWQTpvVHowdnIwQVJXT3NvU1pHSm1rbk9nRm1BRzdKLVlTSDZzMkNwQ1dMeF9IUQ=="; // DEV SANDBOX
#else
                return ConfigurationManager.AppSettings["RingCentral_Base64KeySecret"];
                return "VzBlTXVGcXlSM2VUX2R5ODJrZ2pvUTp3ekYxcVNSSVFrSzVyM0tUN3VFcmZ3VWNGQ3VoRVdRQXlBMEQ0THdKYWlLdw==";
#endif
            }
        }
		public static string TokenUri
		{
			get
			{
#if DEBUG
                return ConfigurationManager.AppSettings["RingCentral_TokenUri"];
                return "https://platform.devtest.ringcentral.com/restapi/oauth/token"; // DEV SANDBOX
#else
                return ConfigurationManager.AppSettings["RingCentral_TokenUri"];
                return "https://platform.ringcentral.com/restapi/oauth/token";
#endif
            }
        }
        public static string ApiUrl
        {
            get
            {
#if DEBUG
                return ConfigurationManager.AppSettings["RingCentral_ApiUrl"];
                return "https://platform.devtest.ringcentral.com"; // DEV SANDBOX
#else
                return ConfigurationManager.AppSettings["RingCentral_ApiUrl"];
                return "https://platform.ringcentral.com";
#endif
            }
        }
        public static string AuthUrl
        {
            get
            {
#if DEBUG
                return ConfigurationManager.AppSettings["RingCentral_AuthUrl"];
                return "https://platform.devtest.ringcentral.com/restapi/oauth/authorize"; // DEV SANDBOX
#else
                return ConfigurationManager.AppSettings["RingCentral_AuthUrl"];
                return "https://platform.ringcentral.com/restapi/oauth/authorize";
#endif
            }
        }
        public static string RedirectUri
		{
			get
			{
#if DEBUG
                return ConfigurationManager.AppSettings["RingCentral_RedirectUri"];
                return "http://localhost:17212/ringcentralauthenticated"; // DEV SANDBOX
#else
                return ConfigurationManager.AppSettings["RingCentral_RedirectUri"];
                return "https://app.ringclone.com/ringcentralauthenticated";
#endif
            }
        }
		public static int AccessTokenLifetime
		{
			get
			{
				return 3600;
			}
		}
	}
}
