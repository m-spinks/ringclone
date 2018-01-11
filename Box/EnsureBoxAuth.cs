using FluentNHibernate.Mapping;
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
using System.Web;
using System.Web.Script.Serialization;
using Box.Models;

namespace Box
{
	public class EnsureBoxAuth
	{
        public BoxAction BoxAction;
        public EnsureBoxAuth(BoxAction boxAction)
        {
            BoxAction = boxAction;
        }
		public void Do(Action action, EnsureBoxAuthModel model)
		{
			Do<object>(() =>
			{
				action();
				return null;
			}, model);
		}

		public T Do<T>(Func<T> action, EnsureBoxAuthModel model)
		{
			T result = default(T);
			try
			{
				result = action();
			}
			catch (WebException ex)
			{
				if (ex.HResult == 401 || ex.Message.Contains("401"))
				{
					refreshAccessToken(model);
					result = action();
				}
				else
				{
					BoxAction.ResultException = ex;
				}
			}
			catch (Exception ex)
			{
                if (ex.Message.Contains("401") || ex.Message.Contains("Unauthorized"))
                {
                    refreshAccessToken(model);
                    result = action();
                }
                else
                {
                    BoxAction.ResultException = ex;
                }
            }
            return result;
		}

		private void refreshAccessToken(EnsureBoxAuthModel model)
		{
			using (ISessionFactory sessionFactory = NHibernateHelper.CreateSessionFactory())
			{
				using (var session = sessionFactory.OpenSession())
				{
					var boxTokenCrit = session.CreateCriteria<BoxTokenRec>();
					boxTokenCrit.Add(Expression.Eq("BoxTokenId", model.BoxTokenId));
					var boxTokens = boxTokenCrit.List<BoxTokenRec>();
					if (boxTokens.Any())
					{
						var boxToken = boxTokens.First();

						string accessToken = boxToken.AccessToken;
						string refreshToken = boxToken.RefreshToken;
						string expiresIn = boxToken.ExpiresIn;
						string tokenType = boxToken.TokenType;
						var t = new Token();
						t.RefreshAccessToken(ref accessToken, ref refreshToken, ref expiresIn, ref tokenType);
						boxToken.AccessToken = accessToken;
						boxToken.RefreshToken = refreshToken;
						boxToken.ExpiresIn = expiresIn;
						boxToken.LastRefreshedOn = DateTime.Now.ToUniversalTime();
						boxToken.TokenType = tokenType;
                        BoxAction.AccessToken = accessToken;
                        BoxAction.RefreshToken = refreshToken;
						using (var transaction = session.BeginTransaction())
						{
							session.SaveOrUpdate(boxToken);
							transaction.Commit();
						}

					}
				}
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
