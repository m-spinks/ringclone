using FluentNHibernate.Mapping;
using NHibernate;
using NHibernate.Criterion;
using RingClone.Portal.Helpers;
using RingClone.Portal.Models;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace RingClone.Portal.Controllers
{
    public class GoogleAuthenticatedController : Controller
    {
		public ActionResult Index(GoogleAuthenticatedModel model)
        {
			//confirm anti-forgery state token
			if (Session["state"] == null || Session["state"].ToString() != model.State)
			{
				throw new HttpException(401, "Auth Failed");
			}
			var stateDecrypted = Helpers.EncryptedString.DatabaseDecrypt(model.State);
			JavaScriptSerializer jss = new JavaScriptSerializer();
			var stateModel = jss.Deserialize<GoogleStateModel>(stateDecrypted);

			if (stateModel == null || string.IsNullOrWhiteSpace(stateModel.User) || stateModel.User != User.Identity.RingCloneIdentity().RingCentralId)
			{
				throw new HttpException(401, "Auth Failed");
			}

			getGoogleToken(model);
			getGoogleEmail(model);
			saveGoogleToken(model);
            saveGoogleAccount(model);

			if (stateModel.ControllerName == "CreateNewRule")
			{
				var createNewRuleModel = jss.Deserialize<CreateNewRuleModel>(stateDecrypted);
				mergeToCreateNewRuleModel(model, createNewRuleModel);
				return RedirectToAction("GoogleAuthenticated", "CreateNewRule", createNewRuleModel);
			}
            if (stateModel.ControllerName == "AdHocTransfer")
            {
                var adHocTransferModel = jss.Deserialize<AdHocTransferModel>(stateDecrypted);
                mergeToAdHocTransferModel(model, adHocTransferModel);
                adHocTransferModel.GoogleTokenId = model.GoogleTokenId;
                return RedirectToAction("GoogleAuthenticated", "AdHocTransfer", adHocTransferModel);
            }
			if (stateModel.ControllerName == "SimpleAutomation")
			{
				var simpleAutomationModel = jss.Deserialize<SimpleAutomationModel>(stateDecrypted);
				mergeToSimpleAutomationModel(model, simpleAutomationModel);
				return RedirectToAction("GoogleAuthenticated", "SimpleAutomation", simpleAutomationModel);
			}
			if (stateModel.ControllerName == "Google")
            {
                return RedirectToAction("Index", "Connections");
            }
            throw new HttpException(500, "Unknown Error");
		}

		private void getGoogleToken(GoogleAuthenticatedModel googleModel)
		{
			var googleTokenUrl = "https://www.googleapis.com/oauth2/v4/token";
			var requestParams = new NameValueCollection();
			requestParams.Add("code", googleModel.Code);
			requestParams.Add("client_id", AppConfig.Google.ClientId);
			requestParams.Add("client_secret", AppConfig.Google.ClientSecret);
			requestParams.Add("redirect_uri", AppConfig.Google.RedirectUri);
			requestParams.Add("grant_type", "authorization_code");
			var array = (from key in requestParams.AllKeys
				 from value in requestParams.GetValues(key)
				 select string.Format("{0}={1}", key, value))
				.ToArray();
			var queryString = string.Join("&", array);
			var postData = queryString;
			byte[] byteArray = Encoding.UTF8.GetBytes(postData);
			WebRequest request = WebRequest.Create(googleTokenUrl);
			request.Method = "POST";
			request.ContentType = "application/x-www-form-urlencoded";
			Stream dataStream = request.GetRequestStream();
			dataStream.Write(byteArray, 0, byteArray.Length);
			dataStream.Close();
			WebResponse response = request.GetResponse();
			dataStream = response.GetResponseStream();
			StreamReader reader = new StreamReader(dataStream);
			string responseFromServer = reader.ReadToEnd();
			JavaScriptSerializer jss = new JavaScriptSerializer();
			var responseModel = jss.Deserialize<GoogleTokenResponse>(responseFromServer);
			googleModel.AccessToken = responseModel.access_token;
			googleModel.IdToken = responseModel.id_token;
			googleModel.ExpiresIn = responseModel.expires_in;
			googleModel.TokenType = responseModel.token_type;
			googleModel.RefreshToken = responseModel.refresh_token;
			reader.Close();
			dataStream.Close();
			response.Close();
		}

		private void getGoogleEmail(GoogleAuthenticatedModel googleModel)
		{
			var googleEmailUrl = "https://www.googleapis.com/userinfo/email";
			var requestParams = new NameValueCollection();
			requestParams.Add("alt", "json");
			requestParams.Add("access_token", googleModel.AccessToken);
			var array = (from key in requestParams.AllKeys
				 from value in requestParams.GetValues(key)
				 select string.Format("{0}={1}", HttpUtility.HtmlEncode(key), HttpUtility.HtmlEncode(value)))
				.ToArray();
			var queryString = string.Join("&", array);
			var url = googleEmailUrl + "?" + queryString;
			WebRequest request = WebRequest.Create(url);
			request.Method = "GET";
			request.ContentType = "application/x-www-form-urlencoded";
			WebResponse response = request.GetResponse();
			Stream dataStream = response.GetResponseStream();
			StreamReader reader = new StreamReader(dataStream);
			string responseFromServer = reader.ReadToEnd();
			JavaScriptSerializer jss = new JavaScriptSerializer();
			var responseModel = jss.Deserialize<GoogleEmailResponse>(responseFromServer);
			googleModel.Email = responseModel.data.email;
			reader.Close();
			dataStream.Close();
			response.Close();
		}

		private void mergeToCreateNewRuleModel(GoogleAuthenticatedModel model, CreateNewRuleModel createNewRuleModel)
		{
			createNewRuleModel.AccessToken = model.AccessToken;
			createNewRuleModel.IdToken = model.IdToken;
			createNewRuleModel.RefreshToken = model.RefreshToken;
			createNewRuleModel.GoogleTokenId = model.GoogleTokenId;
		}

        private void mergeToAdHocTransferModel(GoogleAuthenticatedModel model, AdHocTransferModel adHocTransferModel)
        {
            adHocTransferModel.AccessToken = model.AccessToken;
            adHocTransferModel.RefreshToken = model.RefreshToken;
            adHocTransferModel.GoogleTokenId = model.GoogleTokenId;
            adHocTransferModel.GoogleEmail = model.Email;
        }

		private void mergeToSimpleAutomationModel(GoogleAuthenticatedModel model, SimpleAutomationModel simpleAutomationModel)
		{
			simpleAutomationModel.AccessToken = model.AccessToken;
			simpleAutomationModel.RefreshToken = model.RefreshToken;
			simpleAutomationModel.DestinationGoogleTokenId = model.GoogleTokenId;
			simpleAutomationModel.GoogleEmail = model.Email;
			simpleAutomationModel.DestinationGoogleTokenId = model.GoogleTokenId;
		}

		private void saveGoogleToken(GoogleAuthenticatedModel model)
		{
			using (ISessionFactory sessionFactory = NHibernateHelper.CreateSessionFactory())
			{
				using (var session = sessionFactory.OpenSession())
				{
					GoogleTokenRec tokenRec;
					var tokenCrit = session.CreateCriteria<GoogleTokenRec>();
					tokenCrit.Add(Expression.Eq("Email", model.Email));
					var tokens = tokenCrit.List<GoogleTokenRec>();
					if (tokens.Any())
					{
						tokenRec = tokens.First();
						bool somethingChanged = false;
						if (!string.IsNullOrEmpty(model.RefreshToken))
						{
							tokenRec.LastRefreshedOn = DateTime.Now.ToUniversalTime();
							tokenRec.RefreshToken = model.RefreshToken;
							somethingChanged = true;
						}
						if (!string.IsNullOrEmpty(model.ExpiresIn))
						{
							tokenRec.ExpiresIn = model.ExpiresIn;
							somethingChanged = true;
						}
						if (!string.IsNullOrEmpty(model.AccessToken))
						{
							tokenRec.AccessToken = model.AccessToken;
							somethingChanged = true;
						}
						if (!string.IsNullOrEmpty(model.IdToken))
						{
							tokenRec.IdToken = model.IdToken;
							somethingChanged = true;
						}
						if (!string.IsNullOrEmpty(model.TokenType))
						{
							tokenRec.TokenType = model.TokenType;
							somethingChanged = true;
						}
						if (somethingChanged)
						{
							using (var transaction = session.BeginTransaction())
							{
								session.SaveOrUpdate(tokenRec);
								transaction.Commit();
							}
						}
					}
					else
					{
						tokenRec = new GoogleTokenRec();
						tokenRec.AccessToken = model.AccessToken;
						tokenRec.Email = model.Email;
						tokenRec.ExpiresIn = model.ExpiresIn;
						tokenRec.IdToken = model.IdToken;
						tokenRec.LastRefreshedOn = DateTime.Now.ToUniversalTime();
						tokenRec.RefreshToken = model.RefreshToken;
						tokenRec.TokenType = model.TokenType;
						using (var transaction = session.BeginTransaction())
						{
							session.SaveOrUpdate(tokenRec);
							transaction.Commit();
						}
					}
					model.GoogleTokenId = tokenRec.GoogleTokenId;
				}
			}
		}

        private void saveGoogleAccount(GoogleAuthenticatedModel model)
        {
            using (ISessionFactory sessionFactory = NHibernateHelper.CreateSessionFactory())
            {
                using (var session = sessionFactory.OpenSession())
                {
                    var accountCrit = session.CreateCriteria<AccountRec>();
                    accountCrit.Add(Expression.Eq("RingCentralId", User.Identity.RingCloneIdentity().RingCentralId));
                    var accounts = accountCrit.List<AccountRec>();
                    if (accounts.Any())
                    {
                        var account = accounts.First();
                        var crit = session.CreateCriteria<GoogleAccountRec>();
                        crit.Add(Expression.Eq("AccountId", account.AccountId));
                        crit.Add(Expression.Eq("DeletedInd", false));
                        //crit.Add(Expression.Eq("GoogleTokenId", model.GoogleTokenId));
                        var boxAccounts = crit.List<GoogleAccountRec>();
                        if (boxAccounts.Any())
                        {
                            var boxAccount = boxAccounts.First();
                            boxAccount.GoogleTokenId = model.GoogleTokenId;
                            boxAccount.ActiveInd = true;
                            using (var transaction = session.BeginTransaction())
                            {
                                session.SaveOrUpdate(boxAccount);
                                transaction.Commit();
                            }
                        }
                        else
                        {
                            var boxAccount = new GoogleAccountRec();
                            boxAccount.AccountId = account.AccountId;
                            boxAccount.ActiveInd = true;
                            boxAccount.GoogleAccountName = "";
                            boxAccount.GoogleTokenId = model.GoogleTokenId;
                            using (var transaction = session.BeginTransaction())
                            {
                                session.SaveOrUpdate(boxAccount);
                                transaction.Commit();
                            }
                        }
                    }
                }
            }
        }

        private class AccountRec
        {
            public virtual int AccountId { get; set; }
            public virtual string RingCentralId { get; set; }
        }
        private class AccountRecMap : ClassMap<AccountRec>
        {
            public AccountRecMap()
            {
                Table("T_ACCOUNT");
                Id(x => x.AccountId).Column("AccountId");
                Map(x => x.RingCentralId);
            }
        }
        public class GoogleStateModel
		{
			public string User { get; set; }
			public string ControllerName { get; set; }
			public string AccountType { get; set; }
		}

        public class GoogleAccountRec
        {
            public virtual int GoogleAccountId { get; set; }
            public virtual int GoogleTokenId { get; set; }
            public virtual int AccountId { get; set; }
            public virtual string GoogleAccountName { get; set; }
            public virtual bool DeletedInd { get; set; }
            public virtual bool ActiveInd { get; set; }
        }
        private class GoogleAccountRecMap : ClassMap<GoogleAccountRec>
        {
            public GoogleAccountRecMap()
            {
                Table("T_GOOGLEACCOUNT");
                Id(x => x.GoogleAccountId);
                Map(x => x.GoogleTokenId);
                Map(x => x.GoogleAccountName);
                Map(x => x.AccountId);
                Map(x => x.DeletedInd);
                Map(x => x.ActiveInd);
            }
        }

        public class GoogleTokenRec
		{
			public virtual int GoogleTokenId { get; set; }
			public virtual string Email { get; set; }
			public virtual string AccessToken { get; set; }
			public virtual string IdToken { get; set; }
			public virtual string ExpiresIn { get; set; }
			public virtual string TokenType { get; set; }
			public virtual string RefreshToken { get; set; }
			public virtual DateTime LastRefreshedOn { get; set; }
			public virtual bool DeletedInd { get; set; }
		}
		private class GoogleTokenRecMap : ClassMap<GoogleTokenRec>
		{
			public GoogleTokenRecMap()
			{
				Table("T_GOOGLETOKEN");
				Id(x => x.GoogleTokenId);
				Map(x => x.Email);
				Map(x => x.AccessToken);
				Map(x => x.IdToken);
				Map(x => x.ExpiresIn);
				Map(x => x.TokenType);
				Map(x => x.RefreshToken);
				Map(x => x.DeletedInd);
				Map(x => x.LastRefreshedOn);
			}
		}

		private class GoogleTokenResponse
		{
			public string access_token { get; set; }
			public string id_token { get; set; }
			public string expires_in { get; set; }
			public string token_type { get; set; }
			public string refresh_token { get; set; }
		}
		private class GoogleEmailResponse
		{
			public Data data;
			public class Data
			{
				public string email { get; set; }
			}
		}
	}
}
