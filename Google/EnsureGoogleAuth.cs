using FluentNHibernate.Mapping;
using GoogleActions.Models;
using NHibernate;
using NHibernate.Criterion;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GoogleActions
{
    public class EnsureGoogleAuth
    {
        public GoogleAction GoogleAction;
        public EnsureGoogleAuth(GoogleAction googleAction)
        {
            GoogleAction = googleAction;
        }
        public void Do(Action action, EnsureGoogleAuthModel model)
        {
            Do<object>(() =>
            {
                action();
                return null;
            }, model);
        }

        public T Do<T>(Func<T> action, EnsureGoogleAuthModel model)
        {
            T result = default(T);
            try
            {
                result = action();
            }
            catch (WebException ex)
            {
                var resp = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
                if (ex.HResult == 401 || ex.Message.Contains("401"))
                {
                    refreshAccessToken(model);
                    result = action();
                }
                else
                {
                    GoogleAction.ResultException = ex;
                }
            }
            catch (Exception ex)
            {
                GoogleAction.ResultException = ex;
            }
            return result;
        }

        private void refreshAccessToken(EnsureGoogleAuthModel model)
        {
            using (ISessionFactory sessionFactory = NHibernateHelper.CreateSessionFactory())
            {
                using (var session = sessionFactory.OpenSession())
                {
                    var GoogleTokenCrit = session.CreateCriteria<GoogleTokenRec>();
                    GoogleTokenCrit.Add(Expression.Eq("GoogleTokenId", model.GoogleTokenId));
                    var GoogleTokens = GoogleTokenCrit.List<GoogleTokenRec>();
                    if (GoogleTokens.Any())
                    {
                        var GoogleToken = GoogleTokens.First();

                        string accessToken = GoogleToken.AccessToken;
                        string refreshToken = GoogleToken.RefreshToken;
                        string expiresIn = GoogleToken.ExpiresIn;
                        string tokenType = GoogleToken.TokenType;
                        var t = new Token();
                        t.RefreshAccessToken(ref accessToken, ref refreshToken, ref expiresIn, ref tokenType);
                        GoogleToken.AccessToken = accessToken;
                        if (!string.IsNullOrEmpty(refreshToken))
                            GoogleToken.RefreshToken = refreshToken;
                        if (!string.IsNullOrEmpty(expiresIn))
                            GoogleToken.ExpiresIn = expiresIn;
                        GoogleToken.LastRefreshedOn = DateTime.Now.ToUniversalTime();
                        if (!string.IsNullOrEmpty(tokenType))
                            GoogleToken.TokenType = tokenType;
                        GoogleAction.AccessToken = accessToken;
                        if (!string.IsNullOrEmpty(refreshToken))
                            GoogleAction.RefreshToken = refreshToken;
                        using (var transaction = session.BeginTransaction())
                        {
                            session.SaveOrUpdate(GoogleToken);
                            transaction.Commit();
                        }

                    }
                }
            }
        }
        public class GoogleTokenRec
        {
            public virtual int GoogleTokenId { get; set; }
            public virtual string AccessToken { get; set; }
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
                Table("T_GoogleTOKEN");
                Id(x => x.GoogleTokenId);
                Map(x => x.AccessToken);
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
            public string expires_in { get; set; }
            public string token_type { get; set; }
            public string refresh_token { get; set; }
            public string error { get; set; }
            public string error_description { get; set; }
        }
    }
}
