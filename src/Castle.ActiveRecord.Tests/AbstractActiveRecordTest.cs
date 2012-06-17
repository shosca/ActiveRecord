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


using Castle.ActiveRecord.Scopes;
using Castle.ActiveRecord.Testing;

namespace Castle.ActiveRecord.Tests
{
	using System;

	using NUnit.Framework;
	using Castle.ActiveRecord.Config;

	public abstract class AbstractActiveRecordTest : AbstractTesting
	{
		public override IActiveRecordConfiguration GetConfigSource()
		{
			return System.Configuration.ConfigurationManager.GetSection("activerecord") as IActiveRecordConfiguration;
		}

		[SetUp]
		public override void SetUp()
		{
			base.SetUp();
		}

		[TearDown]
		public override void TearDown()
		{
			base.TearDown();
		}
	}
}
