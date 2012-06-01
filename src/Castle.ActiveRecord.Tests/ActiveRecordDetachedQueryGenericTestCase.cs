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

using System.Linq;
using Castle.ActiveRecord.Scopes;
using Castle.ActiveRecord.Tests.Models;
using NHibernate.Cfg;
using NUnit.Framework;
using NHibernate.Impl;

namespace Castle.ActiveRecord.Tests
{

	[TestFixture]
	public class ActiveRecordDetachedQueryGenericTestCase : AbstractActiveRecordTest
	{
		[SetUp]
		public override void Init()
		{
			ActiveRecord.ResetInitialization();

			ActiveRecord.OnConfigurationCreated += (cfg, sfcfg) =>
				cfg.DataBaseIntegration( db => {
					db.LogSqlInConsole = true;
					db.LogFormattedSql = true;
				}
			);

			ActiveRecord.Initialize(GetConfigSource());
			Recreate();

			using (new SessionScope()) 
			for (int i = 1; i <= 10; i++)
			{
				var blog = new Blog(i) { Name = "n" + i };
				blog.Create();
			}
		}

		[TearDown]
		public override void Drop()
		{
			if (SessionScope.Current != null)
				SessionScope.Current.Dispose();
			base.Drop();
		}

		[Test]
		public void Exists() 
		{

			Assert.AreEqual(10, Blog.FindAll(new DetachedQuery("from Blog")).Count());

			for (int i = 1; i <= 10; i++)
			{
				Assert.AreEqual(true, Blog.Exists(
					new DetachedQuery("from Blog f where f.Id=:value").SetInt32("value", i)));
			}
		}

		[Test]
		public void FindAll() {

			Blog[] list = Blog.FindAll(new DetachedQuery("from Blog Order By Id")).ToArray();

			Assert.AreEqual(10, list.Length);
			Assert.AreEqual(1, list[0].Id);
			Assert.AreEqual("n1", list[0].Name);
			Assert.AreEqual(10, list[9].Id);
			Assert.AreEqual("n10", list[9].Name);
		}

		[Test]
		public void FindOne()
		{
			Blog f = Blog.FindOne(
				new DetachedQuery("from Blog f where f.Id=:value").SetInt32("value", 10));

			Assert.IsNotNull(f);
			Assert.AreEqual(10, f.Id);
			Assert.AreEqual("n10", f.Name);
		}

		[Test]
		public void SlidedFindAll()
		{
			Blog[] list = Blog.SlicedFindAll(5, 9, new DetachedQuery("from Blog")).ToArray();

			Assert.AreEqual(5, list.Length);

			Assert.AreEqual(6, list[0].Id);
			Assert.AreEqual("n6", list[0].Name);

			Assert.AreEqual(10, list[4].Id);
			Assert.AreEqual("n10", list[4].Name);
		}
	}
}
