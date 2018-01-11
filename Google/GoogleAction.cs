using FluentNHibernate.Mapping;
using NHibernate;
using NHibernate.Criterion;
using GoogleActions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleActions
{
    public abstract class GoogleAction
    {
        public string RingCentralId;
        public int GoogleAccountId;
        public string AccessToken;
        public string RefreshToken;
        public Exception ResultException;
        public abstract void DoAction();
        public GoogleAction(string ringCentralId, int googleAccountId)
        {
            RingCentralId = ringCentralId;
            GoogleAccountId = googleAccountId;
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
                        var GoogleAccountCrit = session.CreateCriteria<GoogleAccountRec>();
                        GoogleAccountCrit.Add(Expression.Eq("AccountId", account.AccountId));
                        GoogleAccountCrit.Add(Expression.Eq("GoogleAccountId", GoogleAccountId));
                        var GoogleAccounts = GoogleAccountCrit.List<GoogleAccountRec>();
                        if (GoogleAccounts.Any())
                        {
                            var GoogleAccount = GoogleAccounts.First();
                            var GoogleTokenCrit = session.CreateCriteria<GoogleTokenRec>();
                            GoogleTokenCrit.Add(Expression.Eq("GoogleTokenId", GoogleAccount.GoogleTokenId));
                            var GoogleTokens = GoogleTokenCrit.List<GoogleTokenRec>();
                            if (GoogleTokens.Any())
                            {
                                var GoogleToken = GoogleTokens.First();
                                AccessToken = GoogleToken.AccessToken;
                                RefreshToken = GoogleToken.RefreshToken;
                                var ensureAuth = new EnsureGoogleAuth(this);
                                var ensureAuthModel = new EnsureGoogleAuthModel()
                                {
                                    AccessToken = GoogleToken.AccessToken,
                                    GoogleTokenId = GoogleToken.GoogleTokenId,
                                    ExpiresIn = GoogleToken.ExpiresIn,
                                    LastRefreshedOn = GoogleToken.LastRefreshedOn,
                                    DeletedInd = GoogleToken.DeletedInd,
                                    RefreshToken = GoogleToken.RefreshToken,
                                    TokenType = GoogleToken.TokenType
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
                Table("T_GOOGLETOKEN");
                Id(x => x.GoogleTokenId);
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
