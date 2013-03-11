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


namespace Castle.ActiveRecord.Tests
{
	using System.Configuration;
	using System.IO;
	using System.Linq;
	using System.Reflection;

	using Castle.ActiveRecord.Config;

	using NHibernate.Connection;
	using NHibernate.Dialect;
	using NHibernate.Driver;

	using NUnit.Framework;

	using Configuration = NHibernate.Cfg.Configuration;
	using Environment = NHibernate.Cfg.Environment;

	[TestFixture]
	public class DefaultConfigurationsTestCase
	{
		[Test]
		public void SqlServer2005Defaults()
		{
			var configuration = BuildConfiguration("MsSqlServer2005", "mycs");
			AssertPropertyEquals(configuration, Environment.Dialect, typeof(MsSql2005Dialect).AssemblyQualifiedName);
			AssertPropertyEquals(configuration, Environment.ConnectionProvider, typeof(DriverConnectionProvider).AssemblyQualifiedName);
			AssertPropertyEquals(configuration, Environment.ConnectionDriver, typeof(SqlClientDriver).AssemblyQualifiedName);
			AssertPropertyEquals(configuration, Environment.UseSecondLevelCache, false.ToString());
			AssertPropertyEquals(configuration, Environment.ConnectionStringName, "mycs");
		}

		[Test]
		public void SQLiteDefaults()
		{
			var configuration = BuildConfiguration("SQLite", "sqlite");
			AssertPropertyEquals(configuration, Environment.Dialect, typeof(SQLiteDialect).AssemblyQualifiedName);
			AssertPropertyEquals(configuration, Environment.ConnectionProvider, typeof(DriverConnectionProvider).AssemblyQualifiedName);
			AssertPropertyEquals(configuration, Environment.ConnectionDriver, typeof(SQLite20Driver).AssemblyQualifiedName);
			AssertPropertyEquals(configuration, Environment.UseSecondLevelCache, false.ToString());
			AssertPropertyEquals(configuration, Environment.ConnectionStringName, "sqlite");
			AssertPropertyEquals(configuration, "query.substitutions", "true=1;false=0");
		}

		[Test]
		public void ThrowsWhenConnectionStringNameNotSpecified()
		{
			const string value = @"<activerecord>
	<config database=""MsSqlServer2005"" />
</activerecord>";
			TestDelegate action = () => 

				BuildConfiguration(ReadConfiguration(value));

			var ex = Assert.Throws<ConfigurationErrorsException>(action);
			Assert.AreEqual(
				"Using short form of configuration requires both 'database' and 'connectionStringName' attributes to be specified.",
				ex.Message);
		}
		[Test]
		public void ThrowsWhenDatabaseNotSpecified()
		{
			const string value = @"<activerecord>
	<config connectionStringName=""foobar"" />
</activerecord>";
			TestDelegate action = () =>

				BuildConfiguration(ReadConfiguration(value));

			var ex = Assert.Throws<ConfigurationErrorsException>(action);
			Assert.AreEqual(
				"Using short form of configuration requires both 'database' and 'connectionStringName' attributes to be specified.",
				ex.Message);
		}

		[Test]
		public void ThrowsWhenInvalidDatabaseSpecified()
		{
			const string value = @"<activerecord>
	<config database=""IDontExist!"" connectionStringName=""foobar"" />
</activerecord>";
			TestDelegate action = () =>

				BuildConfiguration(ReadConfiguration(value));

			var ex = Assert.Throws<ConfigurationErrorsException>(action);
			Assert.AreEqual(
				"Specified value (IDontExist!) is not valid for 'database' attribute. " +
				"Valid values are: 'MsSqlServer2000' 'MsSqlServer2005' 'MsSqlServer2008' " +
				"'MsSqlServer2012' 'SQLite' 'MySql' 'MySql5' 'Firebird' 'PostgreSQL' " +
				"'PostgreSQL81' 'PostgreSQL82' 'MsSqlCe' 'Oracle8i' 'Oracle9i' 'Oracle10g'.",
				ex.Message);
		}

		[Test]
		public void CanUseShorthandAttributeForm()
		{
			var value = @"<activerecord>
	<config csn=""foobar"" db=""MsSqlServer2005"">
		<add key=""assembly"" value=""" + Assembly.GetExecutingAssembly().FullName + @""" />
    </config>
</activerecord>";
			var configuration = BuildConfiguration(ReadConfiguration(value));
			AssertPropertyEquals(configuration, Environment.Dialect, typeof(MsSql2005Dialect).AssemblyQualifiedName);
			AssertPropertyEquals(configuration, Environment.ConnectionStringName, "foobar");
		}

		[Test]
		public void CanOverrideDefaults()
		{
			var value = @"<activerecord>
	<config csn=""foobar"" db=""MsSqlServer2005"">
		<add key=""assembly"" value=""" + Assembly.GetExecutingAssembly().FullName + @""" />
		<add key=""cache.use_second_level_cache"" value=""True"" />
	</config>
</activerecord>";
			var configuration = BuildConfiguration(ReadConfiguration(value));
			AssertPropertyEquals(configuration, Environment.UseSecondLevelCache, "True");
		}

		private Configuration BuildConfiguration(string dbName, string csn)
		{
			var source = ReadValidConfiguration(dbName, csn);
			return BuildConfiguration(source);
		}

		private static Configuration BuildConfiguration(IActiveRecordConfiguration source)
		{
			return source.GetConfiguration(string.Empty).BuildConfiguration();
		}

		private static void AssertPropertyEquals(Configuration configuration, string name, string value)
		{
			Assert.AreEqual(value, configuration.Properties[name]);
		}

		private static IActiveRecordConfiguration ReadValidConfiguration(string dbName, string csn)
		{
			var configTemplate = @"<activerecord>
	<config db=""{0}"" csn=""{1}"">
		<add key=""assembly"" value=""" + Assembly.GetExecutingAssembly().FullName + @""" />
    </config>
</activerecord>";
			return ReadConfiguration(string.Format(configTemplate, dbName, csn));
		}

		private static IActiveRecordConfiguration ReadConfiguration(string value)
		{
			using (var reader = new StringReader(value))
			{
				return new XmlActiveRecordConfiguration(reader);
			}
		}
	}
}
