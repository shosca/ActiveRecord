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
	using Castle.ActiveRecord.ByteCode;

	using NHibernate.Connection;
	using NHibernate.Dialect;
	using NHibernate.Driver;

	using NUnit.Framework;

	using Configuration = NHibernate.Cfg.Configuration;

	[TestFixture]
	public class DefaultConfigurationsTestCase
	{
		string cache_use_second_level_cache = "cache.use_second_level_cache";
		string connection_driver_class = "connection.driver_class";
		string connection_provider = "connection.provider";
		string dialect = "dialect";
		string proxyfactory_factory_class = "proxyfactory.factory_class";
		string connection_connection_string_name = "connection.connection_string_name";

		[SetUp]
		public void SetUp()
		{
			ActiveRecord.ResetInitialization();
		}

		[Test]
		public void SqlServer2005Defaults()
		{
			var configuration = BuildConfiguration("MsSqlServer2005", "mycs");
			AssertPropertyEquals(configuration, dialect, typeof(MsSql2005Dialect).AssemblyQualifiedName);
			AssertPropertyEquals(configuration, connection_provider, typeof(DriverConnectionProvider).AssemblyQualifiedName);
			AssertPropertyEquals(configuration, connection_driver_class, typeof(SqlClientDriver).AssemblyQualifiedName);
			AssertPropertyEquals(configuration, proxyfactory_factory_class, typeof(ARProxyFactoryFactory).AssemblyQualifiedName);
			AssertPropertyEquals(configuration, cache_use_second_level_cache, false.ToString());
			AssertPropertyEquals(configuration, connection_connection_string_name, "mycs");
		}

		[Test]
		public void SQLiteDefaults()
		{
			var configuration = BuildConfiguration("SQLite", "sqlite");
			AssertPropertyEquals(configuration, dialect, typeof(SQLiteDialect).AssemblyQualifiedName);
			AssertPropertyEquals(configuration, connection_provider, typeof(DriverConnectionProvider).AssemblyQualifiedName);
			AssertPropertyEquals(configuration, connection_driver_class, typeof(SQLite20Driver).AssemblyQualifiedName);
			AssertPropertyEquals(configuration, proxyfactory_factory_class, typeof(ARProxyFactoryFactory).AssemblyQualifiedName);
			AssertPropertyEquals(configuration, cache_use_second_level_cache, false.ToString());
			AssertPropertyEquals(configuration, connection_connection_string_name, "sqlite");
			AssertPropertyEquals(configuration, "query.substitutions", "true=1;false=0");
		}

		[Test]
		public void Throws_when_connectionStringName_not_specified()
		{
			var value = @"<activerecord>
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
		public void Throws_when_database_not_specified()
		{
			var value = @"<activerecord>
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
		public void Throws_when_invalid_database_specified()
		{
			var value = @"<activerecord>
	<config database=""IDontExist!"" connectionStringName=""foobar"" />
</activerecord>";
			TestDelegate action = () =>

				BuildConfiguration(ReadConfiguration(value));

			var ex = Assert.Throws<ConfigurationErrorsException>(action);
			Assert.AreEqual(
				"Specified value (IDontExist!) is not valid for 'database' attribute. Valid values are: 'MsSqlServer2000' 'MsSqlServer2005' 'MsSqlServer2008' " +
				"'SQLite' 'MySql' 'MySql5' 'Firebird' 'PostgreSQL' 'PostgreSQL81' 'PostgreSQL82' 'MsSqlCe' 'Oracle8i' 'Oracle9i' 'Oracle10g'.",
				ex.Message);
		}

		[Test]
		public void Can_use_shorthand_attribute_form()
		{
			var value = @"<activerecord>
	<config csn=""foobar"" db=""MsSqlServer2005"">
		<add key=""assembly"" value=""" + Assembly.GetExecutingAssembly().FullName + @""" />
    </config>
</activerecord>";
			var configuration = BuildConfiguration(ReadConfiguration(value));
			AssertPropertyEquals(configuration, dialect, typeof(MsSql2005Dialect).AssemblyQualifiedName);
			AssertPropertyEquals(configuration, connection_connection_string_name, "foobar");
		}

		[Test]
		public void Can_override_defaults()
		{
			var value = @"<activerecord>
	<config csn=""foobar"" db=""MsSqlServer2005"">
		<add key=""assembly"" value=""" + Assembly.GetExecutingAssembly().FullName + @""" />
		<add key=""cache.use_second_level_cache"" value=""True"" />
	</config>
</activerecord>";
			var configuration = BuildConfiguration(ReadConfiguration(value));
			AssertPropertyEquals(configuration, cache_use_second_level_cache, "True");
		}

		private Configuration BuildConfiguration(string dbName, string csn)
		{
			var source = ReadValidConfiguration(dbName, csn);
			return BuildConfiguration(source);
		}

		private Configuration BuildConfiguration(IActiveRecordConfiguration source)
		{
			ActiveRecord.Initialize(source);

			return ActiveRecord.Holder.GetAllConfigurations().First();
		}

		private void AssertPropertyEquals(Configuration configuration, string name, string value)
		{
			Assert.AreEqual(value, configuration.Properties[name]);
		}

		private IActiveRecordConfiguration ReadValidConfiguration(string dbName, string csn)
		{
			var configTemplate = @"<activerecord>
	<config db=""{0}"" csn=""{1}"">
		<add key=""assembly"" value=""" + Assembly.GetExecutingAssembly().FullName + @""" />
    </config>
</activerecord>";
			return ReadConfiguration(string.Format(configTemplate, dbName, csn));
		}

		private IActiveRecordConfiguration ReadConfiguration(string value)
		{
			using (var reader = new StringReader(value))
			{
				return new XmlActiveRecordConfiguration(reader);
			}
		}
	}
}
