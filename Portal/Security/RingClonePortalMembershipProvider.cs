using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using FluentNHibernate.Mapping;
using NHibernate;
using NHibernate.Criterion;
using RingClone.Portal.Helpers;

namespace RingClone.Portal.Security
{
	public class RingClonePortalMembershipProvider : MembershipProvider
	{
		public override string ApplicationName
		{
			get
			{
				return "Ring Clone Portal";
			}
			set
			{

			}
		}
		public string ProviderName
		{
			get
			{
				return "RingClonePortalMembershipProvider";
			}

		}
		public override bool ChangePassword(string username, string oldPassword, string newPassword)
		{
			throw new NotImplementedException();
		}

		public override bool ChangePasswordQuestionAndAnswer(string username, string password, string newPasswordQuestion, string newPasswordAnswer)
		{
			throw new NotImplementedException();
		}

		public override MembershipUser CreateUser(string username, string password, string email, string passwordQuestion, string passwordAnswer, bool isApproved, object providerUserKey, out MembershipCreateStatus status)
		{
            throw new NotImplementedException();
        }

        public override bool DeleteUser(string username, bool deleteAllRelatedData)
		{
			throw new NotImplementedException();
		}

		public override bool EnablePasswordReset
		{
			get { throw new NotImplementedException(); }
		}

		public override bool EnablePasswordRetrieval
		{
			get { throw new NotImplementedException(); }
		}

		public override MembershipUserCollection FindUsersByEmail(string emailToMatch, int pageIndex, int pageSize, out int totalRecords)
		{
			throw new NotImplementedException();
		}

		public override MembershipUserCollection FindUsersByName(string usernameToMatch, int pageIndex, int pageSize, out int totalRecords)
		{
			throw new NotImplementedException();
		}

		public override MembershipUserCollection GetAllUsers(int pageIndex, int pageSize, out int totalRecords)
		{
			throw new NotImplementedException();
		}

		public override int GetNumberOfUsersOnline()
		{
			throw new NotImplementedException();
		}

		public override string GetPassword(string username, string answer)
		{
			throw new NotImplementedException();
		}

		public override MembershipUser GetUser(string username, bool userIsOnline)
		{
			throw new NotImplementedException();
		}

		public override MembershipUser GetUser(object providerUserKey, bool userIsOnline)
		{
			throw new NotImplementedException();
		}

		public override string GetUserNameByEmail(string email)
		{
			throw new NotImplementedException();
		}

		public override int MaxInvalidPasswordAttempts
		{
			get { throw new NotImplementedException(); }
		}

		public override int MinRequiredNonAlphanumericCharacters
		{
			get { throw new NotImplementedException(); }
		}

		public override int MinRequiredPasswordLength
		{
			get { throw new NotImplementedException(); }
		}

		public override int PasswordAttemptWindow
		{
			get { throw new NotImplementedException(); }
		}

		public override MembershipPasswordFormat PasswordFormat
		{
			get { throw new NotImplementedException(); }
		}

		public override string PasswordStrengthRegularExpression
		{
			get { throw new NotImplementedException(); }
		}

		public override bool RequiresQuestionAndAnswer
		{
			get { throw new NotImplementedException(); }
		}

		public override bool RequiresUniqueEmail
		{
			get { throw new NotImplementedException(); }
		}

		public override string ResetPassword(string username, string answer)
		{
			throw new NotImplementedException();
		}

		public override bool UnlockUser(string userName)
		{
			throw new NotImplementedException();
		}

		public override void UpdateUser(MembershipUser user)
		{
			throw new NotImplementedException();
		}

		public override bool ValidateUser(string username, string password)
		{
            throw new NotImplementedException();
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
                Map(x => x.RingCentralTokenId);
            }
		}


		public class CreateAccountModel
		{
			public virtual int AccountId { get; set; }
			public virtual string RingCentralId { get; set; }
			public virtual string RingCentralPassword { get; set; }
		}


		private class CreateAccountModelMap : ClassMap<CreateAccountModel>
		{
			public CreateAccountModelMap()
			{
				Table("T_ACCOUNT");
				Id(x => x.AccountId).Column("AccountId");
				Map(x => x.RingCentralId);
				Map(x => x.RingCentralPassword).CustomType<EncryptedString>();
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
            public virtual string Scope { get; set; }
            public virtual string OwnerId { get; set; }
            public virtual string EndpointId { get; set; }
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
                Map(x => x.Scope);
                Map(x => x.OwnerId);
                Map(x => x.EndpointId);
            }
        }

	}
}