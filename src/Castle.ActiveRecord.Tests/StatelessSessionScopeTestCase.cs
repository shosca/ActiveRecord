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


using Iesi.Collections.Generic;

namespace Castle.ActiveRecord.Tests
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Castle.ActiveRecord;
	using Castle.ActiveRecord.Scopes;
	using Castle.ActiveRecord.Tests.Models;
	using NHibernate.Criterion;
	using NUnit.Framework;


	[TestFixture]
	public class StatelessSessionScopeTestCase : AbstractActiveRecordTest
	{
		[Test]
		public void SessionIsStateless()
		{
			Initialize();
			using (new StatelessSessionScope())
			{
				Assert.IsAssignableFrom(typeof(StatelessSessionWrapper), ActiveRecord.Holder.CreateSession(typeof(Blog)));
			}
		}

		[Test]
		public void Unsupported_actions_should_have_squattery_exceptions()
		{
			Initialize();
			using (new StatelessSessionScope())
			{
				Assert.Throws<NotWrappedException>(() =>
					ActiveRecord.Holder.CreateSession(typeof (Blog)).Merge(null)
				);
			}
		}

		[Test]
		public void A_simple_object_can_be_created()
		{
			Initialize();
			using (new StatelessSessionScope())
				CreateBlog();

			var blogs = Blog.FindAll();
			Assert.AreEqual(1, blogs.Count());
			Assert.AreEqual("Mort", blogs.First().Author);
		}

		[Test]
		public void A_simple_object_can_be_read()
		{
			ActiveRecord.Initialize(GetConfigSource());
			Recreate();

			var ship = new Ship() {Name = "Andrea Doria"};

			using (new SessionScope())
				ActiveRecord<Ship>.Create(ship);

			using (new StatelessSessionScope())
			{
				Assert.IsTrue(ActiveRecord<Ship>.Exists(ship.Id));
				Assert.AreEqual("Andrea Doria",ActiveRecord<Ship>.Find(ship.Id).Name);
			}
		}

		[Test]
		public void Get_with_lazy_classes_does_work()
		{
			ActiveRecord.Initialize(GetConfigSource());
			Recreate();

			using (new SessionScope())
				new Blog { Author = "Mort", Name = "Hourglass" }.Create();

			using (new StatelessSessionScope())
			{
				Assert.AreEqual("Mort", Blog.Find(1).Author);
				// The assert below cannot work, stateless sessions cannot serve proxies.
				// Assert.AreEqual(0, Blog.Find(1).Posts.Count);
			}
		}

		[Test]
		public void Updating_stateless_fetched_entities_works()
		{
			InitializeLazy();

			using (new SessionScope())
				CreateLazyBlog();

			using (new StatelessSessionScope())
			{
				var blog = Blog.Find(1);
				Assert.AreEqual("Hourglass", blog.Name);
				blog.Name = "HOURGLASS";
				blog.Update();
			}

			Assert.AreEqual("HOURGLASS", Blog.Find(1).Name);
		}

		[Test]
		public void Updating_detached_entities_works()
		{
			Initialize();
			Blog blog;

			using (new SessionScope())
			{
				CreateBlog();
				blog = Blog.Find(1);
			}

			using (new StatelessSessionScope())
			{
				Assert.AreEqual("Hourglass", blog.Name);
				blog.Name = "HOURGLASS";
				blog.Update();
			}

			Assert.AreEqual("HOURGLASS", Blog.Find(blog.Id).Name);
		}

		[Test]
		public void Inversively_adding_to_a_detached_entitys_collections_works()
		{
			Initialize();
			Blog blog;

			using (new SessionScope())
			{
				CreateBlog();
				blog = Blog.Find(1);
			}

			using (new StatelessSessionScope())
			{
				for (int i = 0; i < 10; i++)
				{
					var post = new Post() { Blog = blog, Title = "Post" + i, Created = DateTime.Now };
					post.Create();
				}
			}

			Assert.AreEqual(10, Post.FindAll().Count());
		}


		[Test]
		public void Updating_detached_entities_collections_does_not_work()
		{
			Initialize();
			Blog blog;

			using (new SessionScope())
			{
				CreateBlog();
				blog = Blog.Find(1);
			}

			using (new StatelessSessionScope())
			{
				blog.Posts.Clear();

				for (int i = 0; i < 10; i++)
				{
					var post = new Post() { Title = "Post" + i, Created = DateTime.Now};
					post.Create();
					blog.Posts.Add(post);
				}

				blog.Update();
			}

			Assert.AreEqual(10, Post.FindAll().Count());
		}

		[Test]
		public void Transactions_are_supported()
		{
			Initialize();

			using (new StatelessSessionScope())
			using (new TransactionScope())
			{
				CreateBlog();
			}

			Assert.AreEqual("Mort", Blog.Find(1).Author);
		}

		[Test]
		public void Querying_works_with_Detached_Criteria()
		{
			InitializeLazy();
			using (new SessionScope())
				CreateLazyBlog();

			var crit = DetachedCriteria.For<Blog>().Add(Expression.Eq("Author", "Mort"));
			using (new StatelessSessionScope())
				Assert.AreEqual(1, ActiveRecord<Blog>.FindAll(crit).Count());

		}


		private void Initialize()
		{
			ActiveRecord.Initialize(GetConfigSource());
			Recreate();
		}

		private void InitializeLazy()
		{
			ActiveRecord.Initialize(GetConfigSource());
			Recreate();
		}

		private void CreateBlog()
		{
			new Blog { Author = "Mort", Name = "Hourglass" }.Create();
		}


		private void CreateLazyBlog()
		{
			new Blog { Author = "Mort", Name = "Hourglass" }.Create();
		}
	}
}
