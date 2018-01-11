using FluentNHibernate.Mapping;
using NHibernate;
using NHibernate.Criterion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmazonActions
{
    public abstract class AmazonAction
    {
        public string RingCentralId;
        public int AmazonAccountId;
        public int AmazonUserId;
        public string AccessKeyId;
        public string SecretAccessKey;
        public string Region;
        public Exception ResultException;
        public abstract void DoAction();
        public AmazonAction(string ringCentralId, int amazonAccountId)
        {
            RingCentralId = ringCentralId;
            AmazonAccountId = amazonAccountId;
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
                        var amazonAccountCrit = session.CreateCriteria<AmazonAccountRec>();
                        amazonAccountCrit.Add(Expression.Eq("AccountId", account.AccountId));
                        amazonAccountCrit.Add(Expression.Eq("AmazonAccountId", AmazonAccountId));
                        var amazonAccounts = amazonAccountCrit.List<AmazonAccountRec>();
                        if (amazonAccounts.Any())
                        {
                            var amazonAccount = amazonAccounts.First();
                            var amazonUserCrit = session.CreateCriteria<AmazonUserRec>();
                            amazonUserCrit.Add(Expression.Eq("AmazonUserId", amazonAccount.AmazonUserId));
                            var amazonUsers = amazonUserCrit.List<AmazonUserRec>();
                            if (amazonUsers.Any())
                            {
                                var amazonUser = amazonUsers.First();
                                AmazonUserId = amazonUser.AmazonUserId;
                                AccessKeyId = amazonUser.AccessKeyId;
                                SecretAccessKey = amazonUser.SecretAccessKey;
                                Region = amazonUser.Region;
                                DoAction();
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
        public class AmazonAccountRec
        {
            public virtual int AmazonAccountId { get; set; }
            public virtual int AmazonUserId { get; set; }
            public virtual int AccountId { get; set; }
            public virtual string AmazonAccountName { get; set; }
            public virtual bool DeletedInd { get; set; }
            public virtual bool ActiveInd { get; set; }
        }
        private class AmazonAccountRecMap : ClassMap<AmazonAccountRec>
        {
            public AmazonAccountRecMap()
            {
                Table("T_AMAZONACCOUNT");
                Id(x => x.AmazonAccountId);
                Map(x => x.AmazonUserId);
                Map(x => x.AmazonAccountName);
                Map(x => x.AccountId);
                Map(x => x.DeletedInd);
                Map(x => x.ActiveInd);
            }
        }
        public class AmazonUserRec
        {
            public virtual int AmazonUserId { get; set; }
            public virtual string Region { get; set; }
            public virtual string AccessKeyId { get; set; }
            public virtual string SecretAccessKey { get; set; }
            public virtual bool DeletedInd { get; set; }
        }
        private class AmazonUserRecMap : ClassMap<AmazonUserRec>
        {
            public AmazonUserRecMap()
            {
                Table("T_AMAZONUSER");
                Id(x => x.AmazonUserId);
                Map(x => x.Region);
                Map(x => x.AccessKeyId);
                Map(x => x.SecretAccessKey);
                Map(x => x.DeletedInd);
            }
        }
    }
}
