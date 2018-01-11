using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Http;
using RingClone.Portal;

namespace RingClone.Portal.Helpers
{
	public class NHibernateHelper
	{
		public static ISessionFactory CreateSessionFactory()
		{
			//var server = HttpContext.Current.Server;
			string connString = ConnectionStringHelper.ConnectionString;
			return Fluently.Configure()
				.Database(MsSqlConfiguration.MsSql2008
					.ShowSql()
					.ConnectionString(connString)
				)
				.Cache(c => c
					.UseQueryCache())
                .Mappings(m => m.FluentMappings.AddFromAssemblyOf<NHibernateHelper>())
				.BuildSessionFactory();
		}

	}

}