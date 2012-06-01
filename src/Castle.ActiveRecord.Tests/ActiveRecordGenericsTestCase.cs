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

namespace Castle.ActiveRecord.Tests
{
	using System.Collections.Generic;
	using System.Threading;
	using NHibernate.Criterion;
	using NUnit.Framework;

	[TestFixture]
	public class ActiveRecordGenericsTestCase : AbstractActiveRecordTest
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
		public void SimpleOperations()
		{
			Blog.DeleteAll();

			Blog[] blogs = Blog.FindAll().ToArray();

			Assert.IsNotNull(blogs);
			Assert.AreEqual(0, blogs.Length);

			Blog blog = new Blog();
			blog.Name = "hammett's blog";
			blog.Author = "hamilton verissimo";
			blog.Save();

			blogs = Blog.FindAll().ToArray();

			Assert.IsNotNull(blogs);
			Assert.AreEqual(1, blogs.Length);

			Blog retrieved = blogs[0];
			Assert.IsNotNull(retrieved);

			Assert.AreEqual(blog.Name, retrieved.Name);
			Assert.AreEqual(blog.Author, retrieved.Author);
		}

		[Test]
		public void ExistsTest()
		{
			Blog.DeleteAll();

			Blog[] blogs = Blog.FindAll().ToArray();

			Assert.IsNotNull(blogs);
			Assert.AreEqual(0, blogs.Length);

			Blog blog = new Blog {Name = "hammett's blog", Author = "hamilton verissimo"};
			blog.Save();

			Assert.IsTrue(blog.Id > 0);
			Assert.IsTrue(Blog.Exists(blog.Id));

			blog = new Blog();
			blog.Name = "chad's blog";
			blog.Author = "chad humphries";
			blog.Save();

			Assert.IsTrue(Blog.Exists(blog.Id));

			Assert.IsFalse(Blog.Exists(1000));
		}

		[Test]
		public void ExistsByCriterion()
		{
			Blog.DeleteAll();

			Blog[] blogs = Blog.FindAll().ToArray();

			Assert.IsNotNull(blogs);
			Assert.AreEqual(0, blogs.Length);

			Blog blog = new Blog();
			blog.Name = "hammett's blog";
			blog.Author = "hamilton verissimo";
			blog.Save();

			Assert.IsTrue(blog.Id > 0);
			Assert.IsTrue(
				Blog.Exists(
					Restrictions.Where<Blog>(b => b.Name == blog.Name),
					Restrictions.Where<Blog>(b => b.Author == blog.Author)
				)
			);

			blog = new Blog();
			blog.Name = "chad's blog";
			blog.Author = "chad humphries";
			blog.Save();

			Assert.IsTrue(
				Blog.Exists(
					Restrictions.Where<Blog>(b => b.Name == blog.Name),
					Restrictions.Where<Blog>(b => b.Author == blog.Author)
				)
			);

			Assert.IsFalse(
				Blog.Exists(
					Restrictions.Where<Blog>(b => b.Name == "/\ndrew's Blog"),
					Restrictions.Where<Blog>(b => b.Author == "Andrew Peters")
				)
			);
		}

		[Test]
		public void BlogExistsCriterionOverload()
		{
			var blog = new Blog()
			           	{
			           		Author = "Dr. Who",
			           		Name = "Exaggerated Murmuring"
			           	};
			blog.SaveAndFlush();

			Assert.IsTrue(
				Blog.Exists(
					Restrictions.Like("Author","Who",MatchMode.Anywhere)
				)
			);
		}

		[Test]
		public void SlicedOperation() 
		{
			Blog.DeleteAll();

			Blog blog = new Blog {Name = "hammett's blog", Author = "hamilton verissimo"};
			blog.Save();

			Post post1 = new Post(blog, "title1", "contents", "category1");
			Post post2 = new Post(blog, "title2", "contents", "category2");
			Post post3 = new Post(blog, "title3", "contents", "category3");

			post1.Save();
			post2.Save();
			post3.Published = true;
			post3.Save();

			Post[] posts = Post.SlicedFindAll(1, 2, Restrictions.Where<Post>(p => p.Blog == blog)).ToArray();
			Assert.AreEqual(2, posts.Length);
		}

		[Test]
		public void SimpleOperations2()
		{
			Blog.DeleteAll();

			Blog[] blogs = Blog.FindAll().ToArray();

			Assert.IsNotNull(blogs);
			Assert.AreEqual(0, blogs.Length);

			Blog blog = new Blog();
			blog.Name = "hammett's blog";
			blog.Author = "hamilton verissimo";
			blog.Create();

			blogs = Blog.FindAll().ToArray();
			Assert.AreEqual(blog.Name, blogs[0].Name);
			Assert.AreEqual(blog.Author, blogs[0].Author);

			Assert.IsNotNull(blogs);
			Assert.AreEqual(1, blogs.Length);

			blog.Name = "something else1";
			blog.Author = "something else2";
			blog.Update();

			blogs = Blog.FindAll().ToArray();

			Assert.IsNotNull(blogs);
			Assert.AreEqual(1, blogs.Length);
			Assert.AreEqual(blog.Name, blogs[0].Name);
			Assert.AreEqual(blog.Author, blogs[0].Author);
		}


		[Test]
		public void ComponentAttribute()
		{
			Company company = new Company("Castle Corp.") {
				Address = new PostalAddress(
				"Embau St., 102", "Sao Paulo", "SP", "040390-060")
			};
			company.Save();

			var companies = Company.FindAll();
			Assert.IsNotNull(companies);
			Assert.AreEqual(1, companies.Count());

			Company corp = companies.First();
			Assert.IsNotNull(corp.Address);
			Assert.AreEqual(corp.Address.Address, company.Address.Address);
			Assert.AreEqual(corp.Address.City, company.Address.City);
			Assert.AreEqual(corp.Address.State, company.Address.State);
			Assert.AreEqual(corp.Address.ZipCode, company.Address.ZipCode);
		}

		[Test]
		public void RelationsOneToMany()
		{
			Blog blog = new Blog {Name = "hammett's blog", Author = "hamilton verissimo"};
			blog.Save();

			Post post1 = new Post(blog, "title1", "contents", "category1");
			Post post2 = new Post(blog, "title2", "contents", "category2");

			post1.Save();
			post2.Save();

			blog.Evict();
			blog = Blog.Find(blog.Id);

			Assert.IsNotNull(blog);
			Assert.IsNotNull(blog.Posts, "posts collection is null");
			Assert.AreEqual(2, blog.Posts.Count);

			foreach(Post post in blog.Posts)
			{
				Assert.AreEqual(blog.Id, post.Blog.Id);
			}
		}

		[Test]
		public void RelationsOneToManyWithWhereAndOrder()
		{
			Blog blog = new Blog {Name = "hammett's blog", Author = "hamilton verissimo"};
			blog.Save();

			Post post1 = new Post(blog, "title1", "contents", "category1");
			Post post2 = new Post(blog, "title2", "contents", "category2");
			Post post3 = new Post(blog, "title3", "contents", "category3");

			post1.Published = false;
			post2.Published = false;
			post3.Published = true;

			post1.Save();
			Thread.Sleep(1000); // Its a smalldatetime (small precision)
			post2.Save();
			Thread.Sleep(1000); // Its a smalldatetime (small precision)
			post3.Save();

			blog = Blog.Find(blog.Id);
			blog.Refresh();

			Assert.IsNotNull(blog);
			Assert.AreEqual(2, blog.UnPublishedPosts.Count);
			Assert.AreEqual(1, blog.PublishedPosts.Count);

			Assert.AreEqual(3, blog.RecentPosts.Count);
			var recentposts = blog.RecentPosts.ToArray();
			Assert.AreEqual(post3.Id, recentposts[2].Id);
			Assert.AreEqual(post2.Id, recentposts[1].Id);
			Assert.AreEqual(post1.Id, recentposts[0].Id);
		}


		[Test]
		public void RelationsOneToOne()
		{
			Employee emp = new Employee {FirstName = "john", LastName = "doe"};
			emp.Save();

			Assert.AreEqual(1, Employee.FindAll().Count());

			Award award = new Award(emp);
			award.Description = "Invisible employee";
			award.Save();

			emp.Award = award;
			emp.Save();

			Assert.AreEqual(1, Award.FindAll().Count());

			Employee emp2 = Employee.Find(emp.Id);
			Assert.IsNotNull(emp2);
			Assert.IsNotNull(emp2.Award);
			Assert.AreEqual(emp.FirstName, emp2.FirstName);
			Assert.AreEqual(emp.LastName, emp2.LastName);
			Assert.AreEqual(award.Description, emp2.Award.Description);
		}

		[Test]
		[ExpectedException(typeof(NotFoundException))]
		public void FindLoad()
		{
			var blog = Blog.Find(1000);
			if (blog == null)
				throw new NotFoundException("");
		}

		[Test]
		public void SaveUpdate()
		{
			Blog.DeleteAll();
			Blog[] blogs = Blog.FindAll().ToArray();

			Assert.IsNotNull(blogs);
			Assert.AreEqual(0, blogs.Length);

			Blog blog = new Blog();
			blog.Name = "hammett's blog";
			blog.Author = "hamilton verissimo";
			blog.Save();

			blogs = Blog.FindAll().ToArray();

			Assert.IsNotNull(blogs);
			Assert.AreEqual(1, blogs.Length);

			blog.Name = "Something else";
			blog.Author = "changed too";
			blog.Save();

			blogs = Blog.FindAll().ToArray();

			Assert.IsNotNull(blogs);
			Assert.AreEqual(1, blogs.Length);

			Assert.AreEqual(blog.Name, blogs[0].Name);
			Assert.AreEqual(blog.Author, blogs[0].Author);
		}

		[Test]
		public void Delete()
		{
			Blog.DeleteAll();
			var blogs = Blog.FindAll();

			Assert.IsNotNull(blogs);
			Assert.AreEqual(0, blogs.Count());

			Blog blog = new Blog {Name = "hammett's blog", Author = "hamilton verissimo"};
			blog.Save();

			blogs = Blog.FindAll();

			Assert.IsNotNull(blogs);
			Assert.AreEqual(1, blogs.Count());

			blog.Delete();

			blogs = Blog.FindAll();

			Assert.IsNotNull(blogs);
			Assert.AreEqual(0, blogs.Count());
		}

		[Test]
		public void UseBlogWithGenericPostCollection()
		{
			Blog blog = new Blog();
			blog.Name = "hammett's blog";
			blog.Author = "hamilton verissimo";

			blog.Save();

			Post p = new Post(blog, "a", "b", "c");
			blog.Posts.Add(p);

			p.Save();

			Blog fromDB = Blog.Find(blog.Id);
			Assert.AreEqual(1, fromDB.Posts.Count);
		}
	}
}
