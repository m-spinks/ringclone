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
using RingCentral.Models;

namespace RingCentral
{
    public class EnsureRingCentralAuth
    {
        public RingCentralAction RingCentralAction;
        public EnsureRingCentralAuth(RingCentralAction ringCentralAction)
        {
            RingCentralAction = ringCentralAction;
        }
        public void Do(Action action, EnsureRingCentralAuthModel model)
        {
            Do<object>(() =>
            {
                action();
                return null;
            }, model);
        }

        public T Do<T>(Func<T> action, EnsureRingCentralAuthModel model)
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
                    var resp = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
                    refreshAccessToken(model);
					result = action();
				}
				else
				{
					RingCentralAction.ResultException = ex;
				}
			}
			catch (Exception ex)
			{
				RingCentralAction.ResultException = ex;
			}
			return result;
		}

        private void refreshAccessToken(EnsureRingCentralAuthModel model)
        {
            using (ISessionFactory sessionFactory = NHibernateHelper.CreateSessionFactory())
            {
                using (var session = sessionFactory.OpenSession())
                {
                    var RingCentralTokenCrit = session.CreateCriteria<RingCentralTokenRec>();
                    RingCentralTokenCrit.Add(Expression.Eq("RingCentralTokenId", model.RingCentralTokenId));
                    var RingCentralTokens = RingCentralTokenCrit.List<RingCentralTokenRec>();
                    if (RingCentralTokens.Any())
                    {
                        var RingCentralToken = RingCentralTokens.First();

                        var t = new RingCentral.Token();
                        var newToken = t.RefreshAccessToken(RingCentralToken.RefreshToken);
                        RingCentralToken.AccessToken = newToken.access_token;
                        RingCentralToken.RefreshToken = newToken.refresh_token;
                        RingCentralToken.ExpiresIn = newToken.expires_in;
                        RingCentralToken.LastRefreshedOn = DateTime.Now.ToUniversalTime();
                        RingCentralToken.RefreshTokenExpiresIn = newToken.refresh_token_expires_in;
                        RingCentralAction.AccessToken = newToken.access_token;
                        RingCentralAction.RefreshToken = newToken.refresh_token;

                        var h = new RingCentralHistoryRec();
                        h.AccessToken = newToken.access_token;
                        h.RefreshToken = newToken.refresh_token;
                        h.ExpiresIn = newToken.expires_in;
                        h.RefreshTokenExpiresIn = newToken.refresh_token_expires_in;
                        h.OwnerId = newToken.owner_id;
                        h.Scope = newToken.scope;
                        h.TokenType = newToken.token_type;
                        h.LastRefreshedOn = DateTime.Now.ToUniversalTime();

                        using (var transaction = session.BeginTransaction())
                        {
                            session.SaveOrUpdate(RingCentralToken);
                            session.SaveOrUpdate(h);
                            transaction.Commit();
                        }
                    }
                }
            }
        }
        public class RingCentralTokenRec
        {
            public virtual int RingCentralTokenId { get; set; }
            public virtual string AccessToken { get; set; }
            public virtual string ExpiresIn { get; set; }
            public virtual string TokenType { get; set; }
            public virtual string RefreshToken { get; set; }
            public virtual string RefreshTokenExpiresIn { get; set; }
            public virtual DateTime LastRefreshedOn { get; set; }
            public virtual bool DeletedInd { get; set; }
        }
        private class RingCentralTokenRecMap : ClassMap<RingCentralTokenRec>
        {
            public RingCentralTokenRecMap()
            {
                Table("T_RINGCENTRALTOKEN");
                Id(x => x.RingCentralTokenId);
                Map(x => x.AccessToken);
                Map(x => x.ExpiresIn);
                Map(x => x.TokenType);
                Map(x => x.RefreshToken);
                Map(x => x.RefreshTokenExpiresIn);
                Map(x => x.DeletedInd);
                Map(x => x.LastRefreshedOn);
            }
        }
        public class RingCentralHistoryRec
        {
            public virtual int RingCentralTokenHistoryId { get; set; }
            public virtual string AccessToken { get; set; }
            public virtual string ExpiresIn { get; set; }
            public virtual string RefreshToken { get; set; }
            public virtual string RefreshTokenExpiresIn { get; set; }
            public virtual string Scope { get; set; }
            public virtual string TokenType { get; set; }
            public virtual string OwnerId { get; set; }
            public virtual DateTime LastRefreshedOn { get; set; }
        }
        private class RingCentralHistoryRecMap : ClassMap<RingCentralHistoryRec>
        {
            public RingCentralHistoryRecMap()
            {
                Table("T_RINGCENTRALTOKENHISTORY");
                Id(x => x.RingCentralTokenHistoryId);
                Map(x => x.AccessToken);
                Map(x => x.ExpiresIn);
                Map(x => x.RefreshToken);
                Map(x => x.RefreshTokenExpiresIn);
                Map(x => x.Scope);
                Map(x => x.TokenType);
                Map(x => x.OwnerId);
                Map(x => x.LastRefreshedOn);
            }
        }
    }
}
