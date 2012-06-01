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
using Castle.ActiveRecord.Tests.Models;
using NHibernate.Cfg;

namespace Castle.ActiveRecord.Tests
{
	using System.Linq;

	using NUnit.Framework;

	[TestFixture]
	public class ActiveRecordLinqTestCase : AbstractActiveRecordTest
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
				var blog = new Blog(i) { Name = "Blog " + i };
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
		public void SimpleOperations()
		{
			Post.DeleteAll();
			Blog.DeleteAll();

			var blogs = from b in Blog.All select b;

			Assert.IsNotNull(blogs);
			Assert.AreEqual(0, blogs.Count());

			var blog = new Blog {
				Name = "hammett's blog",
				Author = "hamilton verissimo"
			};
			blog.Save();

			blogs = from b in Blog.All select b;
			Assert.IsNotNull(blogs);
			Assert.AreEqual(1, blogs.Count());

			var retrieved = Blog.All.First();
			Assert.IsNotNull(retrieved);

			Assert.AreEqual(blog.Name, retrieved.Name);
			Assert.AreEqual(blog.Author, retrieved.Author);
		}

		[Test]
		public void SimpleOperationsShowingBug()
		{
			Post.DeleteAll();
			Blog.DeleteAll();

			var blogs = from b in Blog.All select b;

			Assert.IsNotNull(blogs);
			Assert.AreEqual(0, blogs.Count());

			var blog = new Blog {
				Name = "hammett's blog",
				Author = "hamilton verissimo"
			};
			blog.Save();

			blogs = from b in Blog.All select b;
			Assert.IsNotNull(blogs);
			Assert.AreEqual(1, blogs.Count());

			// this line will fail because of blogs.Count above
			var retrieved = blogs.First();
			Assert.IsNotNull(retrieved);

			Assert.AreEqual(blog.Name, retrieved.Name);
			Assert.AreEqual(blog.Author, retrieved.Author);
		}

		[Test]
		public void SimpleOperations2()
		{
			Post.DeleteAll();
			Blog.DeleteAll();

			var blogs = Blog.All;
			Assert.IsNotNull(blogs);
			Assert.AreEqual(0, blogs.Count());

			Blog blog = new Blog();
			blog.Name = "hammett's blog";
			blog.Author = "hamilton verissimo";
			blog.Create();

			Assert.AreEqual(1, (from b in Blog.All select b).Count());

			blogs = Blog.All;
			Assert.AreEqual(blog.Name, blogs.First().Name);
			Assert.AreEqual(blog.Author, blogs.First().Author);

			blog.Name = "something else1";
			blog.Author = "something else2";
			blog.Update();

			blogs = Blog.All;
			Assert.IsNotNull(blogs);
			Assert.AreEqual(1, Blog.All.Count());
			Assert.AreEqual(blog.Name, blogs.First().Name);
			Assert.AreEqual(blog.Author, blogs.First().Author);
		}

		[Test]
		public void RelationsOneToMany()
		{
			int blogId;
			using (new SessionScope()) {
				Post.DeleteAll();
				Blog.DeleteAll();

				Blog blog0 = new Blog();
				blog0.Name = "hammett's blog";
				blog0.Author = "hamilton verissimo";
				blog0.Save();

				Post post1 = new Post(blog0, "title1", "contents", "category1");
				Post post2 = new Post(blog0, "title2", "contents", "category2");

				post1.Save();
				post2.Save();

				blogId = blog0.Id;
			}


			using (new SessionScope())
			{
				Blog blog = (from b in Blog.All where b.Id == blogId select b).First();

				Blog blog2 = Blog.All.First(b => b.Id == blogId);
				Assert.AreEqual(blog, blog2);

				Blog blog3 = Blog.Find(blogId);
				Assert.AreEqual(blog, blog3);

				Assert.IsNotNull(blog);
				Assert.IsNotNull(blog.Posts, "posts collection is null");
				Assert.AreEqual(2, blog.Posts.Count);

				foreach (Post post in blog.Posts)
				{
					Assert.AreEqual(blog.Id, post.Blog.Id);
				}
			}
		}

		[Test, ExpectedException(typeof(ActiveRecordException))]
		public void Linq_without_session_scope_should_fail()
		{
			var array = ActiveRecord<Blog>.All.ToArray();
		}

		[Test]
		public void Projecting()
		{
			using (new SessionScope())
			{
				var blog = new Blog {Name = "foo", Author = "bar"};
				blog.Save();

				var orderedQueryable = ActiveRecord<Blog>.All;

				var blogs = (from w in orderedQueryable
				               where w.Name.StartsWith("f")
				               select w.Name).ToList();

				Assert.IsNotNull(blogs);
				Assert.AreEqual("foo", blogs.FirstOrDefault());
			}
		}
		[Test]
		public void Projecting2()
		{
			using (new SessionScope())
			{
				var blog = new Blog {Name = "foo", Author = "bar"};
				blog.Save();

				var orderedQueryable = Blog.All;
				var name = (from w in orderedQueryable
				            where w.Name.StartsWith("f")
				            select w.Name).First();

				Assert.IsNotNull(name);
				Assert.AreEqual("foo", name);
			}
		}
	}
}
