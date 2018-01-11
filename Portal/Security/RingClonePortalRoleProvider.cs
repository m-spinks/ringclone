using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using NHibernate;
using NHibernate.Criterion;
using FluentNHibernate.Mapping;
using RingClone.Portal.Helpers;

namespace RingClone.Portal.Security
{
	public class RingClonePortalRoleProvider : RoleProvider
	{
		public override void AddUsersToRoles(string[] usernames, string[] roleNames)
		{
			throw new NotImplementedException();
		}

		public override string ApplicationName
		{
			get { return "Ring Clone Portal"; }
			set { }
		}

		public override void CreateRole(string roleName)
		{
			throw new NotImplementedException();
		}

		public override bool DeleteRole(string roleName, bool throwOnPopulatedRole)
		{
			throw new NotImplementedException();
		}

		public override string[] FindUsersInRole(string roleName, string usernameToMatch)
		{
			throw new NotImplementedException();
		}

		public override string[] GetAllRoles()
		{
			return new string[] { "user" };
		}

		public override string[] GetRolesForUser(string username)
		{
			return new string[] { "user" };
		}

		public override string[] GetUsersInRole(string roleName)
		{
			throw new NotImplementedException();
		}

		public override bool IsUserInRole(string username, string roleName)
		{
			return true;
		}

		public override void RemoveUsersFromRoles(string[] usernames, string[] roleNames)
		{
			throw new NotImplementedException();
		}

		public override bool RoleExists(string roleName)
		{
			throw new NotImplementedException();
		}
	}

	public class AdminUser
	{
		public virtual int UserId { get; set; }
		public virtual string Username { get; set; }
		public virtual int RoleId { get; set; }


	}

	internal class LoginMap : ClassMap<AdminUser>
	{
		public LoginMap()
		{
			Table("T_USER");
			Id(x => x.UserId).Column("UserId");
			Map(x => x.Username).Column("Username");
		}
	}
}