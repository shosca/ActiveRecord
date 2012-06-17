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

using Castle.ActiveRecord.Tests.Model;
using Castle.ActiveRecord.Tests.Models;

namespace Castle.ActiveRecord.Tests.Testing
{
	using System;
	using System.Linq;

	using NUnit.Framework;

	using System.Reflection;
	using System.Collections.Generic;


	[TestFixture]
	public class TypeInitializationTesting : NUnitInMemoryTesting
	{
		public override Assembly[] GetAssemblies()
		{
			return new Assembly[] { typeof(Blog).Assembly };
		}

		[Test]
		public void BasicUsageIsPossible()
		{
			new Blog() { Author = "Me", Name = "Titles" }.Save();
			Assert.AreEqual(1, Blog.FindAll().ToArray().Length);
		}
	}

	[TestFixture]
	public class AdditionalPropertiesInitializationTesting : NUnitInMemoryTesting
	{
		public override Assembly[] GetAssemblies() {
			return new Assembly[] {typeof (Blog).Assembly};
		}

		public override IDictionary<string, string> GetProperties()
		{
			return new Dictionary<string, string> {
				{"show_sql","true"}
			};
		}

		[Test]
		public void PropertiesAreCarriesOver()
		{
			Blog.FindAll();
			var cfg = ActiveRecord.Holder.GetConfiguration(typeof (Blog));
			Assert.AreEqual("true", cfg.Properties["show_sql"]);
		}

	}
}
