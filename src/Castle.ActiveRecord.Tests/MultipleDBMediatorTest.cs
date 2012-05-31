﻿// Copyright 2004-2011 Castle Project - http://www.castleproject.org/
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

namespace Castle.ActiveRecord.Tests
{
	using System.Linq;
	using Castle.ActiveRecord.Scopes;
	using Castle.ActiveRecord.Tests.Models;
	using NUnit.Framework;

	[TestFixture]
	public class MultipleDBMediatorTest : AbstractActiveRecordTest
	{
		[SetUp]
		public void Setup()
		{
			Init();

			ActiveRecord.Initialize(GetConfigSource());

			Recreate();
		}

		[TearDown]
		public override void Drop()
		{
			if (SessionScope.Current != null)
				SessionScope.Current.Dispose();
			base.Drop();
		}

		[Test]
		public void SessionsAreDifferent()
		{
			using (new SessionScope())
			{
				Blog blog = new Blog();
				Author author = new Author();

				ActiveRecord<Blog>.FindAll();
				ActiveRecord<Author>.FindAll();

				Assert.AreNotSame(blog.CurrentSession, author.GetCurrentSession());
				Assert.AreNotEqual(
					blog.CurrentSession.Connection.ConnectionString,
					author.GetCurrentSession().Connection.ConnectionString);
			}
		}

		[Test]
		public void OperateOne() {
			Blog[] blogs = ActiveRecord<Blog>.FindAll().ToArray();

			Assert.AreEqual(0, blogs.Length);

			CreateBlog();

			blogs = ActiveRecord<Blog>.FindAll().ToArray();
			Assert.AreEqual(1, blogs.Length);
		}

		private static void CreateBlog()
		{
			Blog blog = new Blog();
			blog.Name = "Senseless";
			ActiveRecord<Blog>.Save(blog);
		}

		[Test]
		public void OperateTheOtherOne() {
			Author[] authors = ActiveRecord<Author>.FindAll().ToArray();

			Assert.AreEqual(0, authors.Length);

			CreateAuthor();

			authors = ActiveRecord<Author>.FindAll().ToArray();
			Assert.AreEqual(1, authors.Length);
		}

		private static void CreateAuthor()
		{
			Author author = new Author();
			author.Name = "Dr. Who";
			ActiveRecord<Author>.Save(author);
		}

		[Test]
		public void OperateBoth() {
			Blog[] blogs = ActiveRecord<Blog>.FindAll().ToArray();
			Author[] authors = ActiveRecord<Author>.FindAll().ToArray();

			Assert.AreEqual(0, blogs.Length);
			Assert.AreEqual(0, authors.Length);

			CreateBlog();
			CreateAuthor();

			blogs = ActiveRecord<Blog>.FindAll().ToArray();
			authors = ActiveRecord<Author>.FindAll().ToArray();

			Assert.AreEqual(1, blogs.Length);
			Assert.AreEqual(1, authors.Length);
		}
	}
}