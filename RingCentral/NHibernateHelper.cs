using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RingCentral
{
	public class NHibernateHelper
	{
		public static ISessionFactory CreateSessionFactory()
		{
			string connString = RingClone.AppConfig.Database.ConnectionString;
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
