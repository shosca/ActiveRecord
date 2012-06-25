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

using Castle.ActiveRecord.Tests.Models;
using NHibernate.Type;
using NUnit.Framework;

namespace Castle.ActiveRecord.Tests {
	public class ModelTestCase : AbstractActiveRecordTest {
		[SetUp]
		public override void SetUp() {
			base.SetUp();
		}

		[TearDown]
		public override void TearDown()
		{
			base.TearDown();
		}

		[Test]
		public void BlogModelHasIdentifier() {
			var model = AR.Holder.GetModel(typeof (Blog));
			Assert.True(model.PrimaryKey.Key == "Id");
			Assert.True(model.PrimaryKey.Value != null);
			Assert.True(model.PrimaryKey.Value.ReturnedClass == typeof(int));
		}

		[Test]
		public void BlogModelHasNameProperty() {
			var model = AR.Holder.GetModel(typeof (Blog));
			Assert.True(model.Properties.ContainsKey("Name"));
		}

		[Test]
		public void BlogModelHasCollections() {
			var model = AR.Holder.GetModel(typeof (Blog));
			Assert.True(model.HasManys.ContainsKey("Posts"));
		}

		[Test]
		public void PostModelHasBlog() {
			var model = AR.Holder.GetModel(typeof (Post));
			Assert.True(model.BelongsTos.ContainsKey("Blog"));
		}

		[Test]
		public void CompanyModelHasPeopleCollection() {
			var model = AR.Holder.GetModel(typeof (Company));
			Assert.True(model.HasAndBelongsToManys.ContainsKey("People"));
		}

		[Test]
		public void EmployeeModelHasOneToOneAward() {
			var model = AR.Holder.GetModel(typeof (Employee));
			Assert.True(model.OneToOnes.ContainsKey("Award"));
		}
	}
}