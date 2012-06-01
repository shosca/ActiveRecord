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
using NHibernate;
using NHibernate.Cfg;

namespace Castle.ActiveRecord.Tests {
	using System.Collections.Generic;

	using NUnit.Framework;

	using NHibernate.Criterion;

	[TestFixture]
	public class ActiveRecordBaseTestCase : AbstractActiveRecordTest {
		[SetUp]
		public void Setup() {
			ActiveRecord.ResetInitialization();

			ActiveRecord.OnConfigurationCreated += (cfg, sfcfg) =>
				cfg.DataBaseIntegration( db => {
					db.LogSqlInConsole = true;
					db.LogFormattedSql = true;
				}
			);

			ActiveRecord.Initialize(GetConfigSource());

			Recreate();

			Post.DeleteAll();
			Blog.DeleteAll();
			Company.DeleteAll();
			Award.DeleteAll();
			Employee.DeleteAll();
		}

		[TearDown]
		public override void Drop()
		{
			SessionScope.Current.Dispose();
			base.Drop();
		}

		[Test]
		public void SimpleOperations() {
			Blog[] blogs = Blog.FindAll().ToArray();

			Assert.IsNotNull(blogs);
			Assert.AreEqual(0, blogs.Length);

			Blog blog = new Blog {Name = "hammett's blog", Author = "hamilton verissimo"};
			blog.SaveAndFlush();

			blogs = Blog.FindAll().ToArray();

			Assert.IsNotNull(blogs);
			Assert.AreEqual(1, blogs.Length);

			Blog retrieved = blogs.First();
			Assert.IsNotNull(retrieved);

			Assert.AreEqual(blog.Name, retrieved.Name);
			Assert.AreEqual(blog.Author, retrieved.Author);
		}

		[Test]
		public void SlicedOperation() {
			Blog blog = new Blog();
			blog.Name = "hammett's blog";
			blog.Author = "hamilton verissimo";
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
		public void SimpleOperations2() {
			Blog[] blogs = Blog.FindAll().ToArray();

			Assert.IsNotNull(blogs);
			Assert.AreEqual(0, blogs.Length);

			Blog blog = new Blog {Name = "hammett's blog", Author = "hamilton verissimo"};
			blog.CreateAndFlush();

			blogs = Blog.FindAll().ToArray();
			Assert.AreEqual(blog.Name, blogs[0].Name);
			Assert.AreEqual(blog.Author, blogs[0].Author);

			Assert.IsNotNull(blogs);
			Assert.AreEqual(1, blogs.Length);

			blog.Name = "something else1";
			blog.Author = "something else2";
			blog.UpdateAndFlush();

			blogs = Blog.FindAll().ToArray();

			Assert.IsNotNull(blogs);
			Assert.AreEqual(1, blogs.Length);
			blog = blogs.First();
			Assert.AreEqual(blog.Name, blog.Name);
			Assert.AreEqual(blog.Author, blog.Author);
		}

		[Test]
		public void HasManyAndBelongsToMany() {
			var company = new Company("Castle Corp.") {
			Address =
				new PostalAddress("Embau St., 102", "Sao Paulo", "SP", "040390-060")
			};
			company.Save();

			var person = new Person {Name = "ayende"};
			person.Save();

			person.Companies.Add(company);
			company.People.Add(person);
			company.Save();
			person.Save();

			Company fromDB = Company.Find(company.Id);
			Assert.AreEqual(1, fromDB.People.Count);

			Assert.AreEqual("ayende", fromDB.People.First().Name);
		}

		[Test]
		public void ComponentAttribute() {
			Company company = new Company("Castle Corp.");
			company.Address = new PostalAddress(
				"Embau St., 102", "Sao Paulo", "SP", "040390-060");
			company.Save();

			Company[] companies = Company.FindAll().ToArray();
			Assert.IsNotNull(companies);
			Assert.AreEqual(1, companies.Length);

			Company corp = companies[0];
			Assert.IsNotNull(corp.Address);
			Assert.AreEqual(corp.Address.Address, company.Address.Address);
			Assert.AreEqual(corp.Address.City, company.Address.City);
			Assert.AreEqual(corp.Address.State, company.Address.State);
			Assert.AreEqual(corp.Address.ZipCode, company.Address.ZipCode);
		}

		[Test]
		public void RelationsOneToMany() {
			Blog blog = new Blog {Name = "hammett's blog", Author = "hamilton verissimo"};
			blog.Save();

			Post post1 = new Post(blog, "title1", "contents", "category1");
			Post post2 = new Post(blog, "title2", "contents", "category2");

			post1.Save();
			post2.SaveAndFlush();
			blog.Refresh();

			var fromdb = Blog.Peek(blog.Id);

			Assert.IsNotNull(fromdb);
			Assert.IsNotNull(fromdb.Posts, "posts collection is null");
			Assert.AreEqual(2, Post.FindAll().Count());
			Assert.AreEqual(2, fromdb.Posts.Count);

			foreach (Post post in fromdb.Posts)
			{
				Assert.AreEqual(blog.Id, post.Blog.Id);
			}
		}

		[Test]
		public void RelationsOneToManyWithWhereAndOrder() {
			Blog blog = new Blog();
			blog.Name = "hammett's blog";
			blog.Author = "hamilton verissimo";
			blog.Save();

			Post post1 = new Post(blog, "title1", "contents", "category1");
			Post post2 = new Post(blog, "title2", "contents", "category2");
			Post post3 = new Post(blog, "title3", "contents", "category3");

			post1.Save();
			System.Threading.Thread.Sleep(1000); // Its a smalldatetime (small precision)
			post2.Save();
			System.Threading.Thread.Sleep(1000); // Its a smalldatetime (small precision)
			post3.Published = true;
			post3.Save();

			blog = Blog.Find(blog.Id);
			blog.Refresh();

			Assert.IsNotNull(blog);
			Assert.AreEqual(2, blog.UnPublishedPosts.Count);
			Assert.AreEqual(1, blog.PublishedPosts.Count);

			Assert.AreEqual(3, blog.RecentPosts.Count);
			var recentposts = blog.RecentPosts.ToArray();
			Assert.AreEqual(post1.Id, (recentposts[0] as Post).Id);
			Assert.AreEqual(post2.Id, (recentposts[1] as Post).Id);
			Assert.AreEqual(post3.Id, (recentposts[2] as Post).Id);
		}

		[Test]
		public void RelationsOneToOne() {
			Employee emp = new Employee {FirstName = "john", LastName = "doe"};
			emp.Save();

			Assert.AreEqual(1, Employee.FindAll().Count());

			Award award = new Award(emp) {Description = "Invisible employee"};
			award.Save();

			Assert.AreEqual(1, Award.FindAll().Count());

			Employee emp2 = Employee.Find(emp.Id);
			emp2.Refresh();

			Assert.IsNotNull(emp2);
			Assert.IsNotNull(emp2.Award);
			Assert.AreEqual(emp.FirstName, emp2.FirstName);
			Assert.AreEqual(emp.LastName, emp2.LastName);
			Assert.AreEqual(award.Description, emp2.Award.Description);
		}

		[Test]
		[ExpectedException(typeof (NotFoundException))]
		public void FindLoad() {
			var blog = Blog.Find(1);
			if (blog == null)
				throw new NotFoundException("");
		}

		[Test]
		public void SaveUpdate() {
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
		public void Delete() {
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

			blog.Delete();

			blogs = Blog.FindAll().ToArray();

			Assert.IsNotNull(blogs);
			Assert.AreEqual(0, blogs.Length);
		}
	}
}
