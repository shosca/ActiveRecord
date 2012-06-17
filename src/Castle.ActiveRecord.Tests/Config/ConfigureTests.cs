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

using System.Collections.Generic;
using Castle.ActiveRecord.Config;
using Castle.ActiveRecord.Scopes;

namespace Castle.ActiveRecord.Tests.Config
{
	using NUnit.Framework;

	[TestFixture]
	public class ConfigureTests
	{
		[Test]
		public void BasicConfigurationApi() {
			IActiveRecordConfiguration configuration = ActiveRecord.Configure() 
				.Flush(DefaultFlushType.Leave)
				.UseThreadScopeInfo<SampleThreadScopeInfo>()
				.UseSessionFactoryHolder<SampleSessionFactoryHolder>();

			Assert.That(configuration.ThreadScopeInfoImplementation, Is.EqualTo(typeof (SampleThreadScopeInfo)));
			Assert.That(configuration.SessionFactoryHolderImplementation, Is.EqualTo(typeof (SampleSessionFactoryHolder)));
			Assert.That(configuration.DefaultFlushType, Is.EqualTo(DefaultFlushType.Leave));
		}

		[Test, ExpectedException(typeof(ActiveRecordException))]
		public void ThrowExceptionWhenMissingAssemblyFromConfiguration()
		{
			var source = new DefaultActiveRecordConfiguration();

			source.CreateConfiguration(DatabaseType.MsSqlServer2005, "Data Source=.;Initial Catalog=test;Integrated Security=SSPI");

			ActiveRecord.ResetInitialization();

			ActiveRecord.Initialize(source);
		}
	}

	public class SampleThreadScopeInfo : HybridWebThreadScopeInfo
	{
	}

	public class SampleSessionFactoryHolder : SessionFactoryHolder
	{
	}

	public abstract class AuditType
	{
	}

	public class DefaultAuditorType : AuditType
	{
	}

	public class MessagingImpl
	{
	}

	public class OneOfMyEntities
	{
	}

	public class MyMappingClass
	{
	}
}
