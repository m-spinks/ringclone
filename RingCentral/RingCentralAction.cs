using FluentNHibernate.Mapping;
using NHibernate;
using NHibernate.Criterion;
using RingCentral.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RingCentral
{
	public abstract class RingCentralAction
	{
        public string RingCentralId;
        public string AccessToken;
        public string RefreshToken;
        public string Account;
        public string Extension;
		public Exception ResultException;
		public abstract void DoAction();
        public RingCentralAction(string ringCentralId)
        {
            RingCentralId = ringCentralId;
        }
        public void Execute()
        {
            using (ISessionFactory sessionFactory = NHibernateHelper.CreateSessionFactory())
            {
                using (var session = sessionFactory.OpenSession())
                {
                    var accountCrit = session.CreateCriteria<AccountRec>();
                    accountCrit.Add(Expression.Eq("RingCentralId", RingCentralId));
                    var accounts = accountCrit.List<AccountRec>();
                    if (accounts.Any())
                    {
                        var account = accounts.First();
                        var ringCentralTokenCrit = session.CreateCriteria<RingCentralTokenRec>();
                        ringCentralTokenCrit.Add(Expression.Eq("RingCentralTokenId", account.RingCentralTokenId));
                        var ringCentralTokens = ringCentralTokenCrit.List<RingCentralTokenRec>();
                        if (ringCentralTokens.Any())
                        {
                            var ringCentralToken = ringCentralTokens.First();
                            AccessToken = ringCentralToken.AccessToken;
                            RefreshToken = ringCentralToken.RefreshToken;
                            var ensureAuth = new EnsureRingCentralAuth(this);
                            var ensureAuthModel = new EnsureRingCentralAuthModel()
                            {
                                AccessToken = ringCentralToken.AccessToken,
                                RingCentralTokenId = ringCentralToken.RingCentralTokenId,
                                ExpiresIn = ringCentralToken.ExpiresIn,
                                LastRefreshedOn = ringCentralToken.LastRefreshedOn,
                                DeletedInd = ringCentralToken.DeletedInd,
                                RefreshToken = ringCentralToken.RefreshToken,
                                RefreshTokenExpiresIn = ringCentralToken.RefreshTokenExpiresIn,
                                TokenType = ringCentralToken.TokenType
                            };
                            ensureAuth.Do(() => DoAction(), ensureAuthModel);
                        }
                    }
                }
            }
        }

		public class AccountRec
		{
			public virtual int AccountId { get; set; }
            public virtual string RingCentralId { get; set; }
			public virtual string RingCentralExtension { get; set; }
            public virtual int RingCentralTokenId { get; set; }
        }
        private class AccountRecMap : ClassMap<AccountRec>
		{
			public AccountRecMap()
			{
				Table("T_ACCOUNT");
				Id(x => x.AccountId).Column("AccountId");
                Map(x => x.RingCentralId);
                Map(x => x.RingCentralExtension);
                Map(x => x.RingCentralTokenId);
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

	}
}
