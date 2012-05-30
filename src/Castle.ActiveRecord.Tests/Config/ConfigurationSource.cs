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


namespace Castle.ActiveRecord.Tests.Config
{
	using System;
	using System.IO;
	using NUnit.Framework;
	using System.Configuration;

	using Castle.ActiveRecord.Config;
	using Castle.ActiveRecord.Scopes;

	[TestFixture]
	public class ConfigurationSource
	{
		[Test]
		public void TestLoadDefaultThreadScopeConfig()
		{
			String xmlConfig = @"<activerecord>" + GetDefaultHibernateConfigAndCloseActiveRecordSection();

			//null == use default. == typeof(ThreadScopeInfo);
			Type expectedType = null;

			AssertConfig(xmlConfig, expectedType, null);
		}


		[Test]
		public void TestLoadWebThreadScopeInfo()
		{
			String xmlConfig = @"<activerecord isWeb=""true"">" + GetDefaultHibernateConfigAndCloseActiveRecordSection();

			Type expectedType = typeof(WebThreadScopeInfo);

			AssertConfig(xmlConfig, expectedType, null);
		}


		[Test]
		public void TestDefaultSessionFactoryHolder()
		{
			String xmlConfig = @"<activerecord isWeb=""true"">" + GetDefaultHibernateConfigAndCloseActiveRecordSection();

			Type sfh = typeof(SessionFactoryHolder);

			AssertConfig(xmlConfig, null, sfh);
		}


		[Test]
		public void TestCustomSessionFactoryholder()
		{
			String xmlConfig =
				@"<activerecord isWeb=""true"" sessionfactoryholdertype=""Castle.ActiveRecord.Tests.Config.MySessionFactoryHolder, Castle.ActiveRecord.Tests"">"
						+ GetDefaultHibernateConfigAndCloseActiveRecordSection();

			Type sfh = typeof(MySessionFactoryHolder);

			AssertConfig(xmlConfig, null, sfh);
		}


		[Test]
		public void TestDebug()
		{
			String xmlConfig =
				@"<activerecord isDebug=""true"" isWeb=""true"" sessionfactoryholdertype=""Castle.ActiveRecord.Tests.Config.MySessionFactoryHolder, Castle.ActiveRecord.Tests"">"
						+ GetDefaultHibernateConfigAndCloseActiveRecordSection();

			AssertConfig(xmlConfig, null, null, true, false);
		}

		[Test]
		public void TestDefaultFlushType()
		{
			string xmlConfig;
			xmlConfig = @"<activerecord>"
						+ GetDefaultHibernateConfigAndCloseActiveRecordSection();

			AssertConfig(xmlConfig, null, null, false, DefaultFlushType.Classic);

			xmlConfig = @"<activerecord flush=""classic"">"
			+ GetDefaultHibernateConfigAndCloseActiveRecordSection();

			AssertConfig(xmlConfig, null, null, false,DefaultFlushType.Classic);

			xmlConfig = @"<activerecord flush=""auto"">"
			+ GetDefaultHibernateConfigAndCloseActiveRecordSection();

			AssertConfig(xmlConfig, null, null, false, DefaultFlushType.Auto);

			xmlConfig = @"<activerecord flush=""leave"">"
			+ GetDefaultHibernateConfigAndCloseActiveRecordSection();

			AssertConfig(xmlConfig, null, null, false, DefaultFlushType.Leave);

			xmlConfig = @"<activerecord flush=""transaction"">"
			+ GetDefaultHibernateConfigAndCloseActiveRecordSection();

			AssertConfig(xmlConfig, null, null, false, DefaultFlushType.Transaction);

			try
			{
				xmlConfig = @"<activerecord flush=""foo"">" + GetDefaultHibernateConfigAndCloseActiveRecordSection();
				new XmlActiveRecordConfiguration(new StringReader(xmlConfig));
				Assert.Fail("Expected exception not thrown for invalid flush attribute on config");
			}
			catch (Exception ex)
			{
				Assert.IsInstanceOf(typeof(ConfigurationErrorsException), ex);
				Assert.IsTrue(ex.Message.ToLower().Contains("flush"));
				Assert.IsTrue(ex.Message.ToLower().Contains("foo"));
				Assert.IsTrue(ex.Message.ToLower().Contains("classic"));
				Assert.IsTrue(ex.Message.ToLower().Contains("auto"));
				Assert.IsTrue(ex.Message.ToLower().Contains("leave"));
				Assert.IsTrue(ex.Message.ToLower().Contains("transaction"));
			}
		}

		private static void AssertConfig(string xmlConfig, Type webinfotype, Type sessionFactoryHolderType)
		{
			AssertConfig(xmlConfig, webinfotype, sessionFactoryHolderType, false, false);
		}

		private static void AssertConfig(string xmlConfig, Type webinfotype, Type sessionFactoryHolderType, bool isDebug,
										 bool pluralize)
		{
			AssertConfig(xmlConfig, webinfotype, sessionFactoryHolderType, isDebug, pluralize, DefaultFlushType.Classic);
		}

		private static void AssertConfig(string xmlConfig, Type webinfotype, Type sessionFactoryHolderType, bool isDebug,
										 bool pluralize, DefaultFlushType defaultFlushType)
		{
			AssertConfig(xmlConfig, webinfotype, sessionFactoryHolderType, isDebug, defaultFlushType);
		}


		private static void AssertConfig(string xmlConfig, Type webinfotype, Type sessionFactoryHolderType, bool isDebug,
										 DefaultFlushType defaultFlushType)
		{
			StringReader sr = new StringReader(xmlConfig);

			XmlActiveRecordConfiguration c = new XmlActiveRecordConfiguration(sr);

			if (null != webinfotype)
			{
				Assert.IsTrue(c.ThreadScopeInfoImplementation == webinfotype,
							  "Expected {0}, Got {1}", webinfotype, c.ThreadScopeInfoImplementation);
			}

			if (null != sessionFactoryHolderType)
			{
				Assert.IsTrue(c.SessionFactoryHolderImplementation == sessionFactoryHolderType,
							  "Expected {0}, Got {1}", sessionFactoryHolderType, c.SessionFactoryHolderImplementation);
			}

			Assert.IsTrue(c.Debug == isDebug);
			Assert.IsTrue(c.DefaultFlushType == defaultFlushType);
		}

		private static string GetDefaultHibernateConfigAndCloseActiveRecordSection()
		{
			return @"	<config>
							<add key=""connection.driver_class"" value=""NHibernate.Driver.SqlClientDriver"" />
							<add key=""dialect""                 value=""NHibernate.Dialect.MsSql2000Dialect"" />
							<add key=""connection.provider""     value=""NHibernate.Connection.DriverConnectionProvider"" />
							<add key=""connection.connection_string"" value=""Data Source=.;Initial Catalog=test;Integrated Security=True;Pooling=False"" />
						</config>
					</activerecord>";
		}
	}


	public class MySessionFactoryHolder : SessionFactoryHolder
	{
	}
}
