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
    public class BoxAuthenticatedController : Controller
    {
		public ActionResult Index(BoxAuthenticatedModel model)
        {
			//confirm anti-forgery state token
			if (Session["state"] == null || Session["state"].ToString() != model.State)
			{
				throw new HttpException(401, "Auth Failed");
			}
			var stateDecrypted = Helpers.EncryptedString.DatabaseDecrypt(model.State);
			JavaScriptSerializer jss = new JavaScriptSerializer();
			var stateModel = jss.Deserialize<BoxStateModel>(stateDecrypted);

			if (stateModel == null || string.IsNullOrWhiteSpace(stateModel.User) || stateModel.User != User.Identity.RingCloneIdentity().RingCentralId)
			{
				throw new HttpException(401, "Auth Failed");
			}

			getBoxToken(model);
			getBoxEmail(model);
			saveBoxToken(model);
            saveBoxAccount(model);

            if (stateModel.ControllerName == "CreateNewRule")
            {
                var createNewRuleModel = jss.Deserialize<CreateNewRuleModel>(stateDecrypted);
                mergeToCreateNewRuleModel(model, createNewRuleModel);
                return RedirectToAction("BoxAuthenticated", "CreateNewRule", createNewRuleModel);
            }
			if (stateModel.ControllerName == "AdHocTransfer")
			{
				var adHocTransferModel = jss.Deserialize<AdHocTransferModel>(stateDecrypted);
				mergeToAdHocTransferModel(model, adHocTransferModel);
				adHocTransferModel.BoxTokenId = model.BoxTokenId;
				return RedirectToAction("BoxAuthenticated", "AdHocTransfer", adHocTransferModel);
			}
			if (stateModel.ControllerName == "SimpleAutomation")
			{
				var simpleAutomationModel = jss.Deserialize<SimpleAutomationModel>(stateDecrypted);
				mergeToSimpleAutomationModel(model, simpleAutomationModel);
				return RedirectToAction("BoxAuthenticated", "SimpleAutomation", simpleAutomationModel);
			}
			if (stateModel.ControllerName == "Box")
            {
                return RedirectToAction("Index", "Connections");
            }
            throw new HttpException(500, "Unknown Error");
		}

		private void getBoxToken(BoxAuthenticatedModel boxModel)
		{
			var boxTokenUrl = AppConfig.Box.TokenUri;
			var requestParams = new NameValueCollection();
			requestParams.Add("grant_type", "authorization_code");
			requestParams.Add("code", boxModel.Code);
			requestParams.Add("client_id", AppConfig.Box.ClientId);
			requestParams.Add("client_secret", AppConfig.Box.ClientSecret);
			requestParams.Add("redirect_uri", AppConfig.Box.RedirectUri);
			var array = (from key in requestParams.AllKeys
				 from value in requestParams.GetValues(key)
				 select string.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(value)))
				.ToArray();
			var queryString = string.Join("&", array);
			var postData = queryString;
			byte[] byteArray = Encoding.UTF8.GetBytes(postData);
			WebRequest request = WebRequest.Create(boxTokenUrl);
			var err = "";
			try
			{
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
				var responseModel = jss.Deserialize<BoxTokenResponse>(responseFromServer);
				boxModel.AccessToken = responseModel.access_token;
				boxModel.ExpiresIn = responseModel.expires_in;
				boxModel.TokenType = responseModel.token_type;
				boxModel.RefreshToken = responseModel.refresh_token;
				reader.Close();
				dataStream.Close();
				response.Close();
			}
			catch (WebException ex)
			{
				Stream dataStream = ex.Response.GetResponseStream();
				StreamReader reader = new StreamReader(dataStream);
				string responseFromServer = reader.ReadToEnd();
				err = ex.Message;
				throw new WebException(err);
			}

		}

		private void getBoxEmail(BoxAuthenticatedModel boxModel)
		{
			var boxEmailUrl = "https://api.box.com/2.0/users/me";
			var requestParams = new NameValueCollection();
			requestParams.Add("access_token", boxModel.AccessToken);
			var array = (from key in requestParams.AllKeys
				 from value in requestParams.GetValues(key)
				 select string.Format("{0}={1}", key, value))
				.ToArray();
			var queryString = string.Join("&", array);
			var url = boxEmailUrl + "&" + queryString;
			WebRequest request = WebRequest.Create(boxEmailUrl);
			request.Method = "GET";
			request.ContentType = "application/x-www-form-urlencoded";
			request.Headers.Add("Authorization", "Bearer " + boxModel.AccessToken);
			WebResponse response = request.GetResponse();
			Stream dataStream = response.GetResponseStream();
			StreamReader reader = new StreamReader(dataStream);
			string responseFromServer = reader.ReadToEnd();
			JavaScriptSerializer jss = new JavaScriptSerializer();
			var responseModel = jss.Deserialize<BoxEmailResponse>(responseFromServer);
			boxModel.Email = responseModel.login;
			reader.Close();
			dataStream.Close();
			response.Close();
		}

		private void mergeToCreateNewRuleModel(BoxAuthenticatedModel model, CreateNewRuleModel createNewRuleModel)
		{
			createNewRuleModel.AccessToken = model.AccessToken;
			createNewRuleModel.RefreshToken = model.RefreshToken;
			createNewRuleModel.BoxTokenId = model.BoxTokenId;
		}

		private void mergeToAdHocTransferModel(BoxAuthenticatedModel model, AdHocTransferModel adHocTransferModel)
		{
			adHocTransferModel.AccessToken = model.AccessToken;
			adHocTransferModel.RefreshToken = model.RefreshToken;
			adHocTransferModel.BoxTokenId = model.BoxTokenId;
			adHocTransferModel.BoxEmail = model.Email;
		}
		private void mergeToSimpleAutomationModel(BoxAuthenticatedModel model, SimpleAutomationModel simpleAutomationModel)
		{
			simpleAutomationModel.AccessToken = model.AccessToken;
			simpleAutomationModel.RefreshToken = model.RefreshToken;
			simpleAutomationModel.DestinationBoxTokenId = model.BoxTokenId;
			simpleAutomationModel.BoxEmail = model.Email;
			simpleAutomationModel.DestinationBoxTokenId = model.BoxTokenId;
		}

		private void saveBoxToken(BoxAuthenticatedModel model)
		{
			using (ISessionFactory sessionFactory = NHibernateHelper.CreateSessionFactory())
			{
				using (var session = sessionFactory.OpenSession())
				{
					BoxTokenRec tokenRec;
					var tokenCrit = session.CreateCriteria<BoxTokenRec>();
					tokenCrit.Add(Expression.Eq("Email", model.Email));
					var tokens = tokenCrit.List<BoxTokenRec>();
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
						tokenRec = new BoxTokenRec();
						tokenRec.AccessToken = model.AccessToken;
						tokenRec.Email = model.Email;
						tokenRec.ExpiresIn = model.ExpiresIn;
						tokenRec.LastRefreshedOn = DateTime.Now.ToUniversalTime();
						tokenRec.RefreshToken = model.RefreshToken;
						tokenRec.TokenType = model.TokenType;
						using (var transaction = session.BeginTransaction())
						{
							session.SaveOrUpdate(tokenRec);
							transaction.Commit();
						}
					}
					model.BoxTokenId = tokenRec.BoxTokenId;
				}
			}
		}

        private void saveBoxAccount(BoxAuthenticatedModel model)
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
                        var crit = session.CreateCriteria<BoxAccountRec>();
                        crit.Add(Expression.Eq("AccountId", account.AccountId));
                        crit.Add(Expression.Eq("DeletedInd", false));
                        //crit.Add(Expression.Eq("BoxTokenId", model.BoxTokenId));
                        var boxAccounts = crit.List<BoxAccountRec>();
                        if (boxAccounts.Any())
                        {
							var boxAccount = boxAccounts.First();
							boxAccount.BoxTokenId = model.BoxTokenId;
							boxAccount.ActiveInd = true;
							using (var transaction = session.BeginTransaction())
							{
								session.SaveOrUpdate(boxAccount);
								transaction.Commit();
							}
						}
						else 
						{
                            var boxAccount = new BoxAccountRec();
                            boxAccount.AccountId = account.AccountId;
                            boxAccount.ActiveInd = true;
                            boxAccount.BoxAccountName = "";
                            boxAccount.BoxTokenId = model.BoxTokenId;
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
        
        public class BoxStateModel
		{
			public string User { get; set; }
			public string ControllerName { get; set; }
			public string AccountType { get; set; }
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

        public class BoxAccountRec
        {
            public virtual int BoxAccountId { get; set; }
            public virtual int BoxTokenId { get; set; }
            public virtual int AccountId { get; set; }
            public virtual string BoxAccountName { get; set; }
            public virtual bool DeletedInd { get; set; }
            public virtual bool ActiveInd { get; set; }
        }
        private class BoxAccountRecMap : ClassMap<BoxAccountRec>
        {
            public BoxAccountRecMap()
            {
                Table("T_BOXACCOUNT");
                Id(x => x.BoxAccountId);
                Map(x => x.BoxTokenId);
                Map(x => x.BoxAccountName);
                Map(x => x.AccountId);
                Map(x => x.DeletedInd);
                Map(x => x.ActiveInd);
            }
        }
        
        public class BoxTokenRec
		{
			public virtual int BoxTokenId { get; set; }
			public virtual string Email { get; set; }
			public virtual string AccessToken { get; set; }
			public virtual string ExpiresIn { get; set; }
			public virtual string TokenType { get; set; }
			public virtual string RefreshToken { get; set; }
			public virtual DateTime LastRefreshedOn { get; set; }
			public virtual bool DeletedInd { get; set; }
		}
		private class BoxTokenRecMap : ClassMap<BoxTokenRec>
		{
			public BoxTokenRecMap()
			{
				Table("T_BOXTOKEN");
				Id(x => x.BoxTokenId);
				Map(x => x.Email);
				Map(x => x.AccessToken);
				Map(x => x.ExpiresIn);
				Map(x => x.TokenType);
				Map(x => x.RefreshToken);
				Map(x => x.DeletedInd);
				Map(x => x.LastRefreshedOn);
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
		private class BoxEmailResponse
		{
			public string id { get; set; }
			public string name { get; set; }
			public string login { get; set; }
		}
	}
}
