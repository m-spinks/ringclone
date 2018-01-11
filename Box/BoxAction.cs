using FluentNHibernate.Mapping;
using NHibernate;
using NHibernate.Criterion;
using Box.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Box
{
	public abstract class BoxAction
	{
        public string RingCentralId;
        public int BoxAccountId;
        public string AccessToken;
        public string RefreshToken;
		public Exception ResultException;
        public abstract void DoAction();
        public BoxAction(string ringCentralId, int boxAccountId)
        {
            RingCentralId = ringCentralId;
            BoxAccountId = boxAccountId;
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
                        var boxAccountCrit = session.CreateCriteria<BoxAccountRec>();
                        boxAccountCrit.Add(Expression.Eq("AccountId", account.AccountId));
                        boxAccountCrit.Add(Expression.Eq("BoxAccountId", BoxAccountId));
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
                                AccessToken = boxToken.AccessToken;
                                RefreshToken = boxToken.RefreshToken;
                                var ensureAuth = new EnsureBoxAuth(this);
                                var ensureAuthModel = new EnsureBoxAuthModel()
                                {
                                    AccessToken = boxToken.AccessToken,
                                    BoxTokenId = boxToken.BoxTokenId,
                                    ExpiresIn = boxToken.ExpiresIn,
                                    LastRefreshedOn = boxToken.LastRefreshedOn,
                                    DeletedInd = boxToken.DeletedInd,
                                    RefreshToken = boxToken.RefreshToken,
                                    TokenType = boxToken.TokenType
                                };
                                ensureAuth.Do(() => DoAction(), ensureAuthModel);
                            }
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
		}
		private class AccountRecMap : ClassMap<AccountRec>
		{
			public AccountRecMap()
			{
				Table("T_ACCOUNT");
				Id(x => x.AccountId).Column("AccountId");
                Map(x => x.RingCentralId);
                Map(x => x.RingCentralExtension);
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
	}
}
