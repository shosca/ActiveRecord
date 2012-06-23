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

using Castle.ActiveRecord.Config;
using Castle.Core.Configuration;

namespace Castle.ActiveRecord.Testing
{
	using Castle.ActiveRecord;

	/// <summary>
	/// Base class for in memory unit tests. This class does not contain any
	/// attributes specific to a testing framework.
	/// </summary>
	public abstract class InMemoryTesting : AbstractTesting
	{
		public override IActiveRecordConfiguration GetConfigSource() {
			var source = AR.Configure()
				.CreateConfiguration(c => c
					.AddAssemblies(GetAssemblies())
					.SetDatabaseType(DatabaseType.SQLite)
					.ConnectionString("Data Source=:memory:;Version=3;New=True")
					.Set(NHibernate.Cfg.Environment.ConnectionProvider, typeof(InMemoryConnectionProvider).AssemblyQualifiedName)
					.Set(GetProperties())
				);


			return source;
		}

		public override void TearDown() {
			base.TearDown();
			InMemoryConnectionProvider.Restart();
		}
	}
}
