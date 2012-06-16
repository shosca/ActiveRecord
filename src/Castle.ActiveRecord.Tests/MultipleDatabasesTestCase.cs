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
using System.Reflection;
using Castle.ActiveRecord.Scopes;
using Castle.ActiveRecord.Tests.Model;
using Castle.ActiveRecord.Tests.Models;

namespace Castle.ActiveRecord.Tests
{
	using NUnit.Framework;

	[TestFixture]
	public class MultipleDatabasesTestCase : AbstractActiveRecordTest
	{
		[Test]
		public void OperateOne()
		{
			var blogs = Blog.FindAll().ToArray();

			Assert.AreEqual(0, blogs.Length);

			CreateBlog();

			blogs = Blog.FindAll().ToArray();
			Assert.AreEqual(1, blogs.Length);
		}

		private static void CreateBlog()
		{
			var blog = new Blog {Author = "Henry", Name = "Senseless"};

			blog.Save();
		}

		[Test]
		public void OperateTheOtherOne()
		{
			var hands = Hand.FindAll().ToArray();

			Assert.AreEqual(0, hands.Length);

			CreateHand();

			hands = Hand.FindAll().ToArray();

			Assert.AreEqual(1, hands.Length);
		}

		private static void CreateHand()
		{
			var hand = new Hand {Side = "Right"};
			hand.Save();
		}

		[Test]
		public void OperateBoth()
		{
			var blogs = Blog.FindAll().ToArray();
			var hands = Hand.FindAll().ToArray();

			Assert.AreEqual(0, blogs.Length);
			Assert.AreEqual(0, hands.Length);

			CreateBlog();
			CreateHand();

			blogs = Blog.FindAll().ToArray();
			hands = Hand.FindAll().ToArray();

			Assert.AreEqual(1, blogs.Length);
			Assert.AreEqual(1, hands.Length);
		}
	}
}
