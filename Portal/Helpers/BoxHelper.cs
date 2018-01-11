using FluentNHibernate.Mapping;
using NHibernate;
using NHibernate.Criterion;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using System.Web;
using System.Web.Script.Serialization;

namespace RingClone.Portal.Helpers
{
	public class BoxHelper
	{
		public class EnsureBoxAuth
		{
			public void Do(Action action, int boxAccountId, string userIdentityName, ref string accessToken)
			{
				Do<object>(() =>
				{
					action();
					return null;
				}, boxAccountId, userIdentityName, ref accessToken);
			}

			public T Do<T>(Func<T> action, int boxAccountId, string userIdentityName, ref string accessToken)
			{
				try
				{
					return action();
				}
				catch (WebException ex)
				{
					RefreshAccessToken(boxAccountId, userIdentityName, out accessToken);
					return action();
				}
			}

			public void RefreshAccessToken(int boxAccountId, string userIdentityName, out string accessToken)
			{
				accessToken = "";
				using (ISessionFactory sessionFactory = NHibernateHelper.CreateSessionFactory())
				{
					using (var session = sessionFactory.OpenSession())
					{
						var accountCrit = session.CreateCriteria<AccountRec>();
						accountCrit.Add(Expression.Eq("RingCentralId", userIdentityName));
						var accounts = accountCrit.List<AccountRec>();
						if (accounts.Any())
						{
							var account = accounts.First();
							var boxAccountCrit = session.CreateCriteria<BoxAccountRec>();
							boxAccountCrit.Add(Expression.Eq("AccountId", account.AccountId));
							boxAccountCrit.Add(Expression.Eq("BoxAccountId", boxAccountId));
							var boxAccounts = boxAccountCrit.List<BoxAccountRec>();
							if (boxAccounts.Any())
							{
								var boxAccount = boxAccounts.First();
								var boxTokenCrit = session.CreateCriteria<BoxTokenRec>();
								boxTokenCrit.Add(Expression.Eq("BoxTokenId", boxAccount.BoxTokenId));
								var boxTokens = boxTokenCrit.List<BoxTokenRec>();
								if (boxTokens.Any())
								{
									var boxToken = boxTokens.First();
									var boxRefreshTokenUrl = "https://app.box.com/api/oauth2/token/";
									var requestParams = new NameValueCollection();
									requestParams.Add("grant_type", "refresh_token");
									requestParams.Add("refresh_token", boxToken.RefreshToken);
									requestParams.Add("client_id", AppConfig.Box.ClientId);
									requestParams.Add("client_secret", AppConfig.Box.ClientSecret);
									var array = (from key in requestParams.AllKeys
										from value in requestParams.GetValues(key)
										select string.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(value)))
										.ToArray();
									var queryString = string.Join("&", array);
									//var postData = queryString;
									var url = boxRefreshTokenUrl + "?" + queryString;
									WebRequest request = WebRequest.Create(boxRefreshTokenUrl);
									request.Method = "GET";
									request.ContentType = "application/x-www-form-urlencoded";
									//request.Headers.Add("Authorization", "Bearer " + model.AccessToken);
									WebResponse response = request.GetResponse();
									Stream dataStream = response.GetResponseStream();
									StreamReader reader = new StreamReader(dataStream);
									string responseFromServer = reader.ReadToEnd();
									JavaScriptSerializer jss = new JavaScriptSerializer();
									var responseModel = jss.Deserialize<BoxTokenResponse>(responseFromServer);
									reader.Close();
									dataStream.Close();
									response.Close();

									boxToken.AccessToken = responseModel.access_token;
									boxToken.ExpiresIn = responseModel.expires_in;
									boxToken.LastRefreshedOn = DateTime.Now.ToUniversalTime();
									boxToken.RefreshToken = responseModel.refresh_token;
									boxToken.TokenType = responseModel.token_type;

									using (var transaction = session.BeginTransaction())
									{
										session.SaveOrUpdate(boxToken);
										transaction.Commit();
									}

									accessToken = boxToken.AccessToken;

								}
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

	}
}