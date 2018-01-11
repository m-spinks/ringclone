using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using RingClone.Portal.Models;
using NHibernate;
using RingClone.Portal.Helpers;
using NHibernate.Criterion;
using FluentNHibernate.Mapping;
using System.IO;
using System.Web.Script.Serialization;

namespace RingClone.Portal.Api
{
    [Authorize]
    public class FtpConnectionController : ApiController
    {
		[HttpGet]
		public FtpConnectionModel get()
		{
			var model = new FtpConnectionModel();
			model.HasFtpAccount = false;
			model.CanLogin = false;
			try
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
							var crit = session.CreateCriteria<FtpAccountRec>();
							crit.Add(Expression.Eq("AccountId", account.AccountId));
							var ftpAccounts = crit.List<FtpAccountRec>();
							if (ftpAccounts.Any())
							{
								var ftpAccount = ftpAccounts.First();
								model.HasFtpAccount = true;
								model.FtpAccountId = ftpAccount.FtpAccountId;
								model.FtpAccountPassword = "********";
								model.FtpAccountUri = ftpAccount.Uri;
								model.FtpAccountUsername = ftpAccount.Username;

								//TRY TO LOG INTO THE FTP ACCOUNT
								FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpAccount.Uri);
								request.Method = WebRequestMethods.Ftp.ListDirectory;

								// This example assumes the FTP site uses anonymous logon.
								request.Credentials = new NetworkCredential(ftpAccount.Username, ftpAccount.Password);

								FtpWebResponse response = (FtpWebResponse)request.GetResponse();

								Stream responseStream = response.GetResponseStream();
								StreamReader reader = new StreamReader(responseStream);
								string results = reader.ReadToEnd();

								model.CanLogin = true;

								reader.Close();
								response.Close();

							}
						}
					}
				}

			}
			catch (Exception ex)
			{
			}
			return model;
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

		private class TransferRuleRec
		{
			public virtual int TransferRuleId { get; set; }
			public virtual int AccountId { get; set; }
			public virtual int FtpAccountId { get; set; }
			public virtual string Source { get; set; }
			public virtual string Destination { get; set; }
			public virtual string Frequency { get; set; }
			public virtual string DayOf { get; set; }
			public virtual string TimeOfDay { get; set; }
		}
		private class TransferRuleRecMap : ClassMap<TransferRuleRec>
		{
			public TransferRuleRecMap()
			{
				Table("T_TRANSFERRULE");
				Id(x => x.TransferRuleId).Column("TransferRuleId");
				Map(x => x.FtpAccountId);
				Map(x => x.AccountId);
				Map(x => x.Source);
				Map(x => x.Destination);
				Map(x => x.Frequency);
				Map(x => x.DayOf);
				Map(x => x.TimeOfDay);
			}
		}
		private class FtpAccountRec
		{
			public virtual int FtpAccountId { get; set; }
			public virtual string FtpAccountName { get; set; }
			public virtual int AccountId { get; set; }
			public virtual string Uri { get; set; }
			public virtual string Username { get; set; }
			public virtual string Password { get; set; }
		}
		private class FtpAccountRecMap : ClassMap<FtpAccountRec>
		{
			public FtpAccountRecMap()
			{
				Table("T_FTPACCOUNT");
				Id(x => x.FtpAccountId).Column("FtpAccountId");
				Map(x => x.FtpAccountName);
				Map(x => x.AccountId);
				Map(x => x.Password).CustomType<EncryptedString>();
				Map(x => x.Uri);
				Map(x => x.Username);
			}
		}

	}

}
