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
		/// <summary>
		/// Returns dictionary of common properties pre populated with default values for given <paramref name="databaseType"/>.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="databaseType">Database type for which we want default properties.</param>
		/// <param name="connectionstring"></param>
		/// <returns></returns>
		public static SessionFactoryConfig CreateConfiguration(this IActiveRecordConfiguration source, DatabaseType databaseType, string connectionstring) {
			return source.CreateConfiguration(string.Empty, databaseType, connectionstring);
		}

		/// <summary>
		/// Returns dictionary of common properties pre populated with default values for given <paramref name="databaseType"/>.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="name"></param>
		/// <param name="databaseType">Database type for which we want default properties.</param>
		/// <param name="connectionstring"></param>
		/// <returns></returns>
		public static SessionFactoryConfig CreateConfiguration(this IActiveRecordConfiguration source, string name, DatabaseType databaseType, string connectionstring) {
			var config = source.CreateConfiguration(name, databaseType);
			config.Properties[Environment.ConnectionString] = connectionstring;
			return config;
		}

		/// <summary>
		/// Returns dictionary of common properties pre populated with default values for given <paramref name="databaseType"/>.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="databaseType">Database type for which we want default properties.</param>
		/// <returns></returns>
		public static SessionFactoryConfig CreateConfiguration(this IActiveRecordConfiguration source, DatabaseType databaseType) {
			return source.CreateConfiguration(string.Empty, databaseType);
		}

		/// <summary>
		/// Returns dictionary of common properties pre populated with default values for given <paramref name="databaseType"/>.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="name"></param>
		/// <param name="databaseType">Database type for which we want default properties.</param>
		/// <returns></returns>
		public static SessionFactoryConfig CreateConfiguration(this IActiveRecordConfiguration source, string name, DatabaseType databaseType)
		{
			return source.CreateConfiguration(name)
				.SetDatabaseType(databaseType);
		}

		public static SessionFactoryConfig SetDatabaseType(this SessionFactoryConfig config, DatabaseType type) {
			switch (type)
			{
				case DatabaseType.MsSqlServer2000:
					config.Set(Environment.ConnectionDriver, LongName<SqlClientDriver>())
						.Set(Environment.Dialect, LongName<MsSql2000Dialect>());
					break;
				case DatabaseType.MsSqlServer2005:
					config.Set(Environment.ConnectionDriver, LongName<SqlClientDriver>())
						.Set(Environment.Dialect, LongName<MsSql2005Dialect>());
					break;
				case DatabaseType.MsSqlServer2008:
					config.Set(Environment.ConnectionDriver, LongName<SqlClientDriver>())
						.Set(Environment.Dialect, LongName<MsSql2008Dialect>());
					break;
				case DatabaseType.SQLite:
					config.Set(Environment.ConnectionDriver, LongName<SQLite20Driver>())
						.Set(Environment.Dialect, LongName<SQLiteDialect>())
						.Set(SQLite());
					break;
				case DatabaseType.MySql:
					config.Set(Environment.ConnectionDriver, LongName<MySqlDataDriver>())
						.Set(Environment.Dialect, LongName<MySQLDialect>());
					break;
				case DatabaseType.MySql5:
					config.Set(Environment.ConnectionDriver, LongName<MySqlDataDriver>())
						.Set(Environment.Dialect, LongName<MySQL5Dialect>());
					break;
				case DatabaseType.Firebird:
					config.Set(Environment.ConnectionDriver, LongName<FirebirdDriver>())
						.Set(Environment.Dialect, LongName<FirebirdDialect>())
						.Set(Firebird());
					break;
				case DatabaseType.PostgreSQL:
					config.Set(Environment.ConnectionDriver, LongName<NpgsqlDriver>())
						.Set(Environment.Dialect, LongName<PostgreSQLDialect>());
					break;
				case DatabaseType.PostgreSQL81:
					config.Set(Environment.ConnectionDriver, LongName<NpgsqlDriver>())
						.Set(Environment.Dialect, LongName<PostgreSQL81Dialect>());
					break;
				case DatabaseType.PostgreSQL82:
					config.Set(Environment.Dialect, LongName<NpgsqlDriver>())
						.Set(Environment.ConnectionDriver, LongName<PostgreSQL82Dialect>());
					break;
				case DatabaseType.MsSqlCe:
					config.Set(Environment.ConnectionDriver, LongName<SqlServerCeDriver>())
						.Set(Environment.Dialect, LongName<MsSqlCeDialect>())
						.Set(MsSqlCe());
					break;
				// using oracle's own data driver since Microsoft
				// discontinued theirs, and that's what everyone
				// seems to be using anyway.
				case DatabaseType.Oracle8i:
					config.Set(Environment.ConnectionDriver, LongName<OracleDataClientDriver>())
						.Set(Environment.Dialect, LongName<Oracle8iDialect>());
					break;
				case DatabaseType.Oracle9i:
					config.Set(Environment.ConnectionDriver, LongName<OracleDataClientDriver>())
						.Set(Environment.Dialect, LongName<Oracle9iDialect>());
					break;
				case DatabaseType.Oracle10g:
					config.Set(Environment.ConnectionDriver, LongName<OracleDataClientDriver>())
						.Set(Environment.Dialect, LongName<Oracle10gDialect>());
					break;
			}
			return config;
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
				{ Environment.QuerySubstitutions, "true=1;false=0" }
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
				{ Environment.QuerySubstitutions, "true 1, false 0, yes 1, no 0" },
				{ Environment.Isolation, IsolationLevel.ReadCommitted.ToString() },
				{ Environment.CommandTimeout, 444.ToString() },
				{ "use_outer_join", true.ToString() },
			};
		}
	}
}
