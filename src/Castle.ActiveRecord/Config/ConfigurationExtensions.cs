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

namespace Castle.ActiveRecord.Config
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Globalization;
	using System.Reflection;
	using NHibernate.Connection;
	using NHibernate.Dialect;
	using NHibernate.Driver;
	using Environment = NHibernate.Cfg.Environment;

	public static class ConfigurationExtensions
	{
		#region IActiveRecordConfiguration extensions

		public static IActiveRecordConfiguration Flush(this IActiveRecordConfiguration source, DefaultFlushType flushType)
		{
			source.DefaultFlushType = flushType;
			return source;
		}

		public static IActiveRecordConfiguration UseThreadScopeInfo<T>(this IActiveRecordConfiguration source) where T : IThreadScopeInfo 
		{
			return source.UseThreadScopeInfo(typeof (T));
		}

		public static IActiveRecordConfiguration UseThreadScopeInfo(this IActiveRecordConfiguration source, Type type)
		{
			source.ThreadScopeInfoImplementation = type;
			return source;
		}

		public static IActiveRecordConfiguration UseSessionFactoryHolder<T>(this IActiveRecordConfiguration source) where T : ISessionFactoryHolder
		{
			return source.UseSessionFactoryHolder(typeof(T));
		}

		public static IActiveRecordConfiguration UseSessionFactoryHolder(this IActiveRecordConfiguration source, Type type)
		{
			source.SessionFactoryHolderImplementation = type;
			return source;
		}

		public static IActiveRecordConfiguration CreateConfiguration(this IActiveRecordConfiguration source, Action<SessionFactoryConfig> action)
		{
			var cfg = source.CreateConfiguration(string.Empty);
			action(cfg);
			return source;
		}

		public static IActiveRecordConfiguration CreateConfiguration(this IActiveRecordConfiguration source, string name, Action<SessionFactoryConfig> action)
		{
			var cfg = source.CreateConfiguration(name);
			action(cfg);
			return source;
		}

		public static IActiveRecordConfiguration Debug(this IActiveRecordConfiguration source, bool isdebug)
		{
			source.Debug = isdebug;
			return source;
		}

		public static IActiveRecordConfiguration AutoImport(this IActiveRecordConfiguration source, bool autoimport)
		{
			source.AutoImport = autoimport;
			return source;
		}

		public static IActiveRecordConfiguration Lazy(this IActiveRecordConfiguration source, bool lazy)
		{
			source.Lazy = lazy;
			return source;
		}

		#endregion

		#region SessionFactoryConfig extensions

		public static SessionFactoryConfig AddContributor(this SessionFactoryConfig config, INHContributor contributor) {
			config.Contributors.Add(contributor);
			return config;
		}

		public static SessionFactoryConfig Set(this SessionFactoryConfig config, IDictionary<string, string> properties) {
			foreach (var property in properties) {
				config.Set(property.Key, property.Value);
			}
			return config;
		}

		public static SessionFactoryConfig Set(this SessionFactoryConfig config, string key, string value) {
			config.Properties[key] = value;
			return config;
		}

		public static SessionFactoryConfig AddAssembly(this SessionFactoryConfig config, Assembly assembly) {
			config.Assemblies.Add(assembly);
			return config;
		}

		public static SessionFactoryConfig AddAssemblies(this SessionFactoryConfig config, IEnumerable<Assembly> assemblies) {
			foreach (var asm in assemblies) {
				config.Assemblies.Add(asm);
			}
			return config;
		}

		public static SessionFactoryConfig CreateConfiguration(this IActiveRecordConfiguration source, string name) {
			source.Add(new SessionFactoryConfig(source) {Name = name});
			return source.GetConfiguration(name)
				.Set(Environment.ConnectionProvider, typeof(DriverConnectionProvider).AssemblyQualifiedName)
				.Set(Environment.UseSecondLevelCache, false.ToString(CultureInfo.InvariantCulture))
				;
		}

		public static SessionFactoryConfig ConnectionString(this SessionFactoryConfig config, string connectionstring) {
			return config.Set(Environment.ConnectionString, connectionstring);
		}

		public static SessionFactoryConfig Dialect<T>(this SessionFactoryConfig config) where T : Dialect
		{
			return config.Set(Environment.Dialect, LongName<T>());
		}

		public static SessionFactoryConfig ConnectionDriver<T>(this SessionFactoryConfig config) where T : IDriver 
		{
			return config.Set(Environment.ConnectionDriver, LongName<T>());
		}

		public static SessionFactoryConfig SetDatabaseType(this SessionFactoryConfig config, DatabaseType type) {
			switch (type)
			{
				case DatabaseType.MsSqlServer2000:
					config
						.ConnectionDriver<SqlClientDriver>()
						.Dialect<MsSql2000Dialect>();
					break;
				case DatabaseType.MsSqlServer2005:
					config
						.ConnectionDriver<SqlClientDriver>()
						.Dialect<MsSql2005Dialect>();
					break;
				case DatabaseType.MsSqlServer2008:
					config
						.ConnectionDriver<SqlClientDriver>()
						.Dialect<MsSql2008Dialect>();
					break;
				case DatabaseType.SQLite:
					config
						.ConnectionDriver<SQLite20Driver>()
						.Dialect<SQLiteDialect>()
						.Set(SQLite());
					break;
				case DatabaseType.MySql:
					config
						.ConnectionDriver<MySqlDataDriver>()
						.Dialect<MySQLDialect>();
					break;
				case DatabaseType.MySql5:
					config
						.ConnectionDriver<MySqlDataDriver>()
						.Dialect<MySQL5Dialect>();
					break;
				case DatabaseType.Firebird:
					config
						.ConnectionDriver<FirebirdDriver>()
						.Dialect<FirebirdDialect>()
						.Set(Firebird());
					break;
				case DatabaseType.PostgreSQL:
					config
						.ConnectionDriver<NpgsqlDriver>()
						.Dialect<PostgreSQLDialect>();
					break;
				case DatabaseType.PostgreSQL81:
					config
						.ConnectionDriver<NpgsqlDriver>()
						.Dialect<PostgreSQL81Dialect>();
					break;
				case DatabaseType.PostgreSQL82:
					config
						.ConnectionDriver<NpgsqlDriver>()
						.Dialect<PostgreSQL82Dialect>();
					break;
				case DatabaseType.MsSqlCe:
					config
						.ConnectionDriver<SqlServerCeDriver>()
						.Dialect<MsSqlCeDialect>()
						.Set(MsSqlCe());
					break;
				// using oracle's own data driver since Microsoft
				// discontinued theirs, and that's what everyone
				// seems to be using anyway.
				case DatabaseType.Oracle8i:
					config
						.ConnectionDriver<OracleDataClientDriver>()
						.Dialect<Oracle8iDialect>();
					break;
				case DatabaseType.Oracle9i:
					config
						.ConnectionDriver<OracleDataClientDriver>()
						.Dialect<Oracle9iDialect>();
					break;
				case DatabaseType.Oracle10g:
					config
						.ConnectionDriver<OracleDataClientDriver>()
						.Dialect<Oracle10gDialect>();
					break;
			}
			return config;
		}

		#endregion

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
