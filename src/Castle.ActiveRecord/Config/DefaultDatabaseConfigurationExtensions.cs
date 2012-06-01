// Copyright 2004-2011 Castle Project - http://www.castleproject.org/
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using Castle.ActiveRecord.ByteCode;
using NHibernate.Connection;
using NHibernate.Dialect;
using NHibernate.Driver;
using Environment = NHibernate.Cfg.Environment;

namespace Castle.ActiveRecord.Config
{
	/// <summary>
	/// Exposes default configuration properties for common databases defined in <see cref="DatabaseType"/> enum.
	/// </summary>
	public static class DefaultDatabaseConfigurationExtensions
	{
		private const string connection_driver_class = "connection.driver_class";
		private const string connection_isolation = "connection.isolation";
		private const string connection_provider = "connection.provider";
		private const string dialect = "dialect";
		private const string proxyfactory_factory_class = "proxyfactory.factory_class";
		private const string query_substitutions = "query.substitutions";

		public static SessionFactoryConfig CreateConfiguration(this IActiveRecordConfiguration source, DatabaseType databaseType, string connectionstring) {
			var config = source.CreateConfiguration(databaseType);
			config.Properties[Environment.ConnectionString] = connectionstring;
			return config;
		}

		/// <summary>
		/// Returns dictionary of common properties pre populated with default values for given <paramref name="databaseType"/>.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="databaseType">Database type for which we want default properties.</param>
		/// <returns></returns>
		public static SessionFactoryConfig CreateConfiguration(this IActiveRecordConfiguration source, DatabaseType databaseType)
		{
			switch (databaseType)
			{
				case DatabaseType.MsSqlServer2000:
					return source.CreateConfiguration<SqlClientDriver, MsSql2000Dialect>();
				case DatabaseType.MsSqlServer2005:
					return source.CreateConfiguration<SqlClientDriver, MsSql2005Dialect>();
				case DatabaseType.MsSqlServer2008:
					return source.CreateConfiguration<SqlClientDriver, MsSql2008Dialect>();
				case DatabaseType.SQLite:
					return source.CreateConfiguration<SQLite20Driver, SQLiteDialect>(SQLite());
				case DatabaseType.MySql:
					return source.CreateConfiguration<MySqlDataDriver, MySQLDialect>();
				case DatabaseType.MySql5:
					return source.CreateConfiguration<MySqlDataDriver, MySQL5Dialect>();
				case DatabaseType.Firebird:
					return source.CreateConfiguration<FirebirdDriver, FirebirdDialect>(Firebird());
				case DatabaseType.PostgreSQL:
					return source.CreateConfiguration<NpgsqlDriver, PostgreSQLDialect>();
				case DatabaseType.PostgreSQL81:
					return source.CreateConfiguration<NpgsqlDriver, PostgreSQL81Dialect>();
				case DatabaseType.PostgreSQL82:
					return source.CreateConfiguration<NpgsqlDriver, PostgreSQL82Dialect>();
				case DatabaseType.MsSqlCe:
					return source.CreateConfiguration<SqlServerCeDriver, MsSqlCeDialect>(MsSqlCe());
				// using oracle's own data driver since Microsoft
				// discontinued theirs, and that's what everyone
				// seems to be using anyway.
				case DatabaseType.Oracle8i:
					return source.CreateConfiguration<OracleDataClientDriver, Oracle8iDialect>();
				case DatabaseType.Oracle9i:
					return source.CreateConfiguration<OracleDataClientDriver, Oracle9iDialect>();
				case DatabaseType.Oracle10g:
					return source.CreateConfiguration<OracleDataClientDriver, Oracle10gDialect>();
			}

			throw new ArgumentOutOfRangeException("databaseType", databaseType, "Unsupported database type");
		}

		static SessionFactoryConfig CreateConfiguration<TDriver, TDialect>(this IActiveRecordConfiguration source)
			where TDriver : IDriver
			where TDialect : Dialect
		{
			return source.CreateConfiguration<TDriver, TDialect>(new Dictionary<string, string>());
		}

		static SessionFactoryConfig CreateConfiguration<TDriver, TDialect>(this IActiveRecordConfiguration source, Dictionary<string, string> configuration)
			where TDriver : IDriver
			where TDialect : Dialect {
			var sfconfig = source.CreateConfiguration();
			sfconfig.Properties[connection_provider] = LongName<DriverConnectionProvider>();
			sfconfig.Properties[Environment.UseSecondLevelCache] = false.ToString(CultureInfo.InvariantCulture);
			sfconfig.Properties[proxyfactory_factory_class] = LongName<ARProxyFactoryFactory>();
			sfconfig.Properties[dialect] = LongName<TDialect>();
			sfconfig.Properties[connection_driver_class] = LongName<TDriver>();
			foreach (var d in configuration) {
				sfconfig.Properties[d.Key] = d.Value;

			}
			return sfconfig;
		}

		static string LongName<TType>()
		{
			return typeof(TType).AssemblyQualifiedName;
		}

		static Dictionary<string, string> SQLite()
		{
			// based on https://www.hibernate.org/361.html#A9
			return new Dictionary<string, string>
			{
				{ query_substitutions, "true=1;false=0" }
			};
		}

		static Dictionary<string, string> MsSqlCe()
		{
			// to workaround exception being thrown with default setting
			// when an implicit transaction is used with identity id
			// see: AR-ISSUE-273 for details
			return new Dictionary<string, string>
			{
				{ Environment.ReleaseConnections, "on_close" }
			};
		}

		static Dictionary<string, string> Firebird()
		{
			// based on https://www.hibernate.org/361.html#A5
			return new Dictionary<string, string>
			{
				{ query_substitutions, "true 1, false 0, yes 1, no 0" },
				{ connection_isolation, IsolationLevel.ReadCommitted.ToString() },
				{ "command_timeout", 444.ToString() },
				{ "use_outer_join", true.ToString() },
			};
		}
	}
}
