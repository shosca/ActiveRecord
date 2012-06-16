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
using NHibernate.Impl;

namespace Castle.ActiveRecord.Tests
{
	using System.Threading;
	using NHibernate.Criterion;
	using NUnit.Framework;

	[TestFixture]
	public class ActiveRecordTestCase : AbstractActiveRecordTest
	{
		[SetUp]
		public override void Init()
		{
			base.Init();

			using (new SessionScope()) 
			for (var i = 1; i <= 10; i++)
			{
				var blog = new Blog(i) { Name = "n" + i };
				blog.Create();
			}
		}

		[Test]
		public void SimpleOperations()
		{
			Blog.DeleteAll();

			var blogs = Blog.FindAll().ToArray();

			Assert.IsNotNull(blogs);
			Assert.AreEqual(0, blogs.Length);

			var blog = new Blog {Name = "hammett's blog", Author = "hamilton verissimo"};
			blog.Save();

			blogs = Blog.FindAll().ToArray();

			Assert.IsNotNull(blogs);
			Assert.AreEqual(1, blogs.Length);

			var retrieved = blogs[0];
			Assert.IsNotNull(retrieved);

			Assert.AreEqual(blog.Name, retrieved.Name);
			Assert.AreEqual(blog.Author, retrieved.Author);
		}

		[Test]
		public void SimpleOperations1() {
			var blogs = Blog.FindAll().ToArray();

			Assert.IsNotNull(blogs);
			Assert.AreEqual(10, blogs.Length);

			var blog = new Blog {Name = "hammett's blog", Author = "hamilton verissimo"};
			blog.SaveAndFlush();

			blogs = Blog.FindAll().ToArray();

			Assert.IsNotNull(blogs);
			Assert.AreEqual(11, blogs.Length);

			var retrieved = Blog.Find(blog.Id);
			Assert.IsNotNull(retrieved);

			Assert.AreEqual(blog.Name, retrieved.Name);
			Assert.AreEqual(blog.Author, retrieved.Author);
		}

		[Test]
		public void SimpleOperations2()
		{
			Blog.DeleteAll();

			var blogs = Blog.FindAll().ToArray();

			Assert.IsNotNull(blogs);
			Assert.AreEqual(0, blogs.Length);

			var blog = new Blog {Name = "hammett's blog", Author = "hamilton verissimo"};
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
		public void SimpleOperations3() {
			var blogs = Blog.FindAll().ToArray();

			Assert.IsNotNull(blogs);
			Assert.AreEqual(10, blogs.Length);

			var blog = new Blog {Name = "hammett's blog", Author = "hamilton verissimo"};
			blog.CreateAndFlush();

			blogs = Blog.FindAll().ToArray();
			Assert.AreEqual(blog.Name, blogs[10].Name);
			Assert.AreEqual(blog.Author, blogs[10].Author);

			Assert.IsNotNull(blogs);
			Assert.AreEqual(11, blogs.Length);

			blog.Name = "something else1";
			blog.Author = "something else2";
			blog.UpdateAndFlush();

			blogs = Blog.FindAll().ToArray();

			Assert.IsNotNull(blogs);
			Assert.AreEqual(11, blogs.Length);
			blog = blogs.Last();
			Assert.AreEqual(blog.Name, blog.Name);
			Assert.AreEqual(blog.Author, blog.Author);
		}

		[Test]
		public void ExistsTest()
		{
			var blogs = Blog.FindAll().ToArray();

			Assert.IsNotNull(blogs);
			Assert.AreEqual(10, blogs.Length);

			var blog = new Blog {Name = "hammett's blog", Author = "hamilton verissimo"};
			blog.Save();

			Assert.IsTrue(blog.Id > 0);
			Assert.IsTrue(Blog.Exists(blog.Id));

			blog = new Blog {Name = "chad's blog", Author = "chad humphries"};
			blog.Save();

			Assert.IsTrue(Blog.Exists(blog.Id));

			Assert.IsFalse(Blog.Exists(1000));
		}

		[Test]
		public void ExistsByCriterion()
		{
			var blogs = Blog.FindAll().ToArray();

			Assert.IsNotNull(blogs);
			Assert.AreEqual(10, blogs.Length);

			const string name = "hammett's blog";
			const string author = "hamilton verissimo";

			var blog = new Blog {Name = name, Author = author};
			blog.Save();

			Assert.IsTrue(blog.Id > 0);
			Assert.IsTrue(
				Blog.Exists(
					Restrictions.Where<Blog>(b => b.Name == name),
					Restrictions.Where<Blog>(b => b.Author == author)
				)
			);

			blog = new Blog {Name = "chad's blog", Author = "chad humphries"};
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
			var blog = new Blog { Author = "Dr. Who", Name = "Exaggerated Murmuring" };
			blog.SaveAndFlush();

			Assert.IsTrue(
				Blog.Exists(
					Restrictions.Like("Author", "Who", MatchMode.Anywhere)
				)
			);
		}

		[Test]
		public void SlicedOperation() 
		{
			var blog = new Blog {Name = "hammett's blog", Author = "hamilton verissimo"};
			blog.Save();

			var post1 = new Post(blog, "title1", "contents", "category1");
			var post2 = new Post(blog, "title2", "contents", "category2");
			var post3 = new Post(blog, "title3", "contents", "category3");

			post1.Save();
			post2.Save();
			post3.Published = true;
			post3.Save();

			var posts = Post.SlicedFindAll(1, 2, Restrictions.Where<Post>(p => p.Blog == blog)).ToArray();
			Assert.AreEqual(2, posts.Length);
		}

		[Test]
		public void ComponentAttribute()
		{
			var company = new Company("Castle Corp.") {
				Address = new PostalAddress(
				"Embau St., 102", "Sao Paulo", "SP", "040390-060")
			};
			company.Save();

			var companies = Company.FindAll().ToArray();
			Assert.IsNotNull(companies);
			Assert.AreEqual(1, companies.Length);

			var corp = companies.First();
			Assert.IsNotNull(corp.Address);
			Assert.AreEqual(corp.Address.Address, company.Address.Address);
			Assert.AreEqual(corp.Address.City, company.Address.City);
			Assert.AreEqual(corp.Address.State, company.Address.State);
			Assert.AreEqual(corp.Address.ZipCode, company.Address.ZipCode);
		}

		[Test]
		public void RelationsOneToMany() {
			var blog = new Blog {Name = "hammett's blog", Author = "hamilton verissimo"};
			blog.Save();

			var post1 = new Post(blog, "title1", "contents", "category1");
			var post2 = new Post(blog, "title2", "contents", "category2");

			post1.Save();
			post2.SaveAndFlush();
			blog.Refresh();

			var fromdb = Blog.Peek(blog.Id);

			Assert.IsNotNull(fromdb);
			Assert.IsNotNull(fromdb.Posts, "posts collection is null");
			Assert.AreEqual(2, Post.FindAll().Count());
			Assert.AreEqual(2, fromdb.Posts.Count);

			foreach (var post in fromdb.Posts)
			{
				Assert.AreEqual(blog.Id, post.Blog.Id);
			}
		}

		[Test]
		public void RelationsOneToManyWithWhereAndOrder()
		{
			var blog = new Blog {Name = "hammett's blog", Author = "hamilton verissimo"};
			blog.Save();

			var post1 = new Post(blog, "title1", "contents", "category1");
			var post2 = new Post(blog, "title2", "contents", "category2");
			var post3 = new Post(blog, "title3", "contents", "category3");

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
			var emp = new Employee {FirstName = "john", LastName = "doe"};
			emp.Save();

			Assert.AreEqual(1, Employee.FindAll().Count());

			var award = new Award(emp) {Description = "Invisible employee"};
			award.Save();

			emp.Award = award;
			emp.Save();

			Assert.AreEqual(1, Award.FindAll().Count());

			var emp2 = Employee.Find(emp.Id);
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
			var blogs = Blog.FindAll().ToArray();

			Assert.IsNotNull(blogs);
			Assert.AreEqual(10, blogs.Length);

			var blog = new Blog {Name = "hammett's blog", Author = "hamilton verissimo"};
			blog.Save();

			blogs = Blog.FindAll().ToArray();

			Assert.IsNotNull(blogs);
			Assert.AreEqual(11, blogs.Length);

			blog.Name = "Something else";
			blog.Author = "changed too";
			blog.Save();

			blogs = Blog.FindAll().ToArray();

			Assert.IsNotNull(blogs);
			Assert.AreEqual(11, blogs.Length);

			var fromdb = blogs.Last();
			Assert.AreEqual(blog.Name, fromdb.Name);
			Assert.AreEqual(blog.Author, fromdb.Author);
		}

		[Test]
		public void Delete()
		{
			var blogs = Blog.FindAll();

			Assert.IsNotNull(blogs);
			Assert.AreEqual(10, blogs.Count());

			var blog = new Blog {Name = "hammett's blog", Author = "hamilton verissimo"};
			blog.Save();

			blogs = Blog.FindAll();

			Assert.IsNotNull(blogs);
			Assert.AreEqual(11, blogs.Count());

			blog.Delete();

			blogs = Blog.FindAll();

			Assert.IsNotNull(blogs);
			Assert.AreEqual(10, blogs.Count());
		}

		[Test]
		public void DeleteWithExpression()
		{
			var blog = new Blog {Name = "hammett's blog", Author = "hamilton verissimo"};
			blog.Save();

			Blog.DeleteAll(b => b.Author == "hamilton verissimo");

			var blogs = Blog.FindAll();
			Assert.IsNotNull(blogs);
			Assert.AreEqual(10, blogs.Count());
		}

		[Test]
		public void DeleteWithCriteria()
		{
			var blog = new Blog {Name = "hammett's blog", Author = "hamilton verissimo"};
			blog.Save();

			Blog.DeleteAll(Restrictions.Where<Blog>(b => b.Author == "hamilton verissimo"));

			var blogs = Blog.FindAll();
			Assert.IsNotNull(blogs);
			Assert.AreEqual(10, blogs.Count());
		}

		[Test]
		public void DeleteWithQueryOver()
		{
			var blog = new Blog {Name = "hammett's blog", Author = "hamilton verissimo"};
			blog.Save();

			Blog.DeleteAll(QueryOver.Of<Blog>().Where(b => b.Author == "hamilton verissimo"));

			var blogs = Blog.FindAll();
			Assert.IsNotNull(blogs);
			Assert.AreEqual(10, blogs.Count());
		}

		[Test]
		public void DeleteWithCriteriaExtension()
		{
			var blog = new Blog {Name = "hammett's blog", Author = "hamilton verissimo"};
			blog.Save();

			DetachedCriteria.For<Blog>()
				.Add(Restrictions.Where<Blog>(b => b.Author == "hamilton verissimo"))
				.DeleteAll<Blog>();

			var blogs = Blog.FindAll();
			Assert.IsNotNull(blogs);
			Assert.AreEqual(10, blogs.Count());
		}

		[Test]
		public void DeleteWithQueryOverExtension()
		{
			var blog = new Blog {Name = "hammett's blog", Author = "hamilton verissimo"};
			blog.Save();

			QueryOver.Of<Blog>().Where(b => b.Author == "hamilton verissimo").DeleteAll();

			var blogs = Blog.FindAll();
			Assert.IsNotNull(blogs);
			Assert.AreEqual(10, blogs.Count());
		}

		[Test]
		public void UseBlogWithGenericPostCollection()
		{
			var blog = new Blog {Name = "hammett's blog", Author = "hamilton verissimo"};

			blog.Save();

			var p = new Post(blog, "a", "b", "c");
			blog.Posts.Add(p);

			p.Save();

			var fromDB = Blog.Find(blog.Id);
			Assert.AreEqual(1, fromDB.Posts.Count);
		}

		[Test]
		public void ExistsCriterionOverload()
		{
			var blog = new Blog { Author = "Dr. Who", Name = "Exaggerated Murmuring" };
			blog.SaveAndFlush();

			Assert.IsTrue(
				Blog.Exists(
					Restrictions.Like("Author", "Who", MatchMode.Anywhere)
				)
			);
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
		public void LinqSimpleOperations()
		{
			new SessionScope();
			var blogs = from b in Blog.All select b;

			Assert.IsNotNull(blogs);
			Assert.AreEqual(10, blogs.Count());

			var blog = new Blog {
				Name = "hammett's blog",
				Author = "hamilton verissimo"
			};
			blog.Save();

			blogs = from b in Blog.All select b;
			Assert.IsNotNull(blogs);
			Assert.AreEqual(11, blogs.Count());

			var retrieved = Blog.All.ToArray().Last();
			Assert.IsNotNull(retrieved);

			Assert.AreEqual(blog.Name, retrieved.Name);
			Assert.AreEqual(blog.Author, retrieved.Author);
		}

		[Test]
		public void LinqSimpleOperationsShowingBug()
		{
			using (new SessionScope())
			{
				var blogs = from b in Blog.All select b;

				Assert.IsNotNull(blogs);
				Assert.AreEqual(10, blogs.Count());

				var blog = new Blog {
					Name = "hammett's blog",
					Author = "hamilton verissimo"
				};
				blog.Save();

				blogs = from b in Blog.All orderby b.Id descending select b;
				Assert.IsNotNull(blogs);
				Assert.AreEqual(11, blogs.Count());

				// this line will fail because of blogs.Count above
				var retrieved = blogs.First();
				Assert.IsNotNull(retrieved);

				Assert.AreEqual(blog.Name, retrieved.Name);
				Assert.AreEqual(blog.Author, retrieved.Author);
			}
		}

		[Test]
		public void LinqSimpleOperations2()
		{
			new SessionScope();
			var blogs = Blog.All;
			Assert.IsNotNull(blogs);
			Assert.AreEqual(10, blogs.Count());

			var blog = new Blog {Name = "hammett's blog", Author = "hamilton verissimo"};
			blog.Create();

			Assert.AreEqual(11, (from b in Blog.All select b).Count());

			blogs = Blog.All.OrderByDescending(b => b.Id);
			Assert.AreEqual(blog.Name, blogs.First().Name);
			Assert.AreEqual(blog.Author, blogs.First().Author);

			blog.Name = "something else1";
			blog.Author = "something else2";
			blog.Update();

			blogs = Blog.All.OrderByDescending(b => b.Id);
			Assert.IsNotNull(blogs);
			Assert.AreEqual(11, Blog.All.Count());
			Assert.AreEqual(blog.Name, blogs.First().Name);
			Assert.AreEqual(blog.Author, blogs.First().Author);
		}

		[Test]
		public void LinqRelationsOneToMany()
		{
			int blogId;
			using (new SessionScope()) {
				Post.DeleteAll();
				Blog.DeleteAll();

				var blog0 = new Blog {Name = "hammett's blog", Author = "hamilton verissimo"};
				blog0.Save();

				var post1 = new Post(blog0, "title1", "contents", "category1");
				var post2 = new Post(blog0, "title2", "contents", "category2");

				post1.Save();
				post2.Save();

				blogId = blog0.Id;
			}


			using (new SessionScope())
			{
				var blog = (from b in Blog.All where b.Id == blogId select b).First();

				var blog2 = Blog.All.First(b => b.Id == blogId);
				Assert.AreEqual(blog, blog2);

				var blog3 = Blog.Find(blogId);
				Assert.AreEqual(blog, blog3);

				Assert.IsNotNull(blog);
				Assert.IsNotNull(blog.Posts, "posts collection is null");
				Assert.AreEqual(2, blog.Posts.Count);

				foreach (var post in blog.Posts)
				{
					Assert.AreEqual(blog.Id, post.Blog.Id);
				}
			}
		}

		[Test, ExpectedException(typeof(ActiveRecordException))]
		public void LinqWithoutSessionScopeShouldFail()
		{
			var array = ActiveRecord<Blog>.All.ToArray();
			if (array.Length > 0) return;
		}

		[Test]
		public void LinqProjecting()
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
		public void LinqProjecting2()
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

		[Test]
		public void ExistsDetachedQuery() 
		{

			Assert.AreEqual(10, Blog.FindAll(new DetachedQuery("from Blog")).Count());

			for (int i = 1; i <= 10; i++)
			{
				Assert.AreEqual(true, Blog.Exists(
					new DetachedQuery("from Blog f where f.Id=:value").SetInt32("value", i)));
			}
		}

		[Test]
		public void FindAllDetachedQuery() {

			Blog[] list = Blog.FindAll(new DetachedQuery("from Blog Order By Id")).ToArray();

			Assert.AreEqual(10, list.Length);
			Assert.AreEqual(1, list[0].Id);
			Assert.AreEqual("n1", list[0].Name);
			Assert.AreEqual(10, list[9].Id);
			Assert.AreEqual("n10", list[9].Name);
		}

		[Test]
		public void FindOneDetachedQuery()
		{
			Blog f = Blog.FindOne(
				new DetachedQuery("from Blog f where f.Id=:value").SetInt32("value", 10));

			Assert.IsNotNull(f);
			Assert.AreEqual(10, f.Id);
			Assert.AreEqual("n10", f.Name);
		}

		[Test]
		public void SlidedFindAllDetachedQuery()
		{
			Blog[] list = Blog.SlicedFindAll(5, 9, new DetachedQuery("from Blog")).ToArray();

			Assert.AreEqual(5, list.Length);

			Assert.AreEqual(6, list[0].Id);
			Assert.AreEqual("n6", list[0].Name);

			Assert.AreEqual(10, list[4].Id);
			Assert.AreEqual("n10", list[4].Name);
		}

		[Test]
		public void LifecycleMethods()
		{
			var blog = new Blog {Name = "hammett's blog", Author = "hamilton verissimo"};

			Assert.IsFalse(blog.OnSaveCalled());
			Assert.IsFalse(blog.OnDeleteCalled());
			Assert.IsFalse(blog.OnLoadCalled());
			Assert.IsFalse(blog.OnUpdateCalled());

			blog.SaveAndFlush();

			Assert.IsTrue(blog.OnSaveCalled());
			Assert.IsFalse(blog.OnDeleteCalled());
			Assert.IsFalse(blog.OnLoadCalled());
			Assert.IsFalse(blog.OnUpdateCalled());

			blog.Name = "hammett's blog x";
			blog.Author = "hamilton verissimo x";
			blog.SaveAndFlush();
			Assert.IsTrue(blog.OnUpdateCalled());
			blog.Evict();

			blog = Blog.Find(blog.Id);
			Assert.IsTrue(blog.OnLoadCalled());

			blog.Delete();
			Assert.IsTrue(blog.OnDeleteCalled());
		}

		[Test]
		public void FindByPropertyTestName()
		{
			var blog = new Blog {Name = null, Author = "hamilton verissimo"};
			blog.Save();

			var blogs = Blog.FindAllByProperty("Name", null).ToArray();

			Assert.IsTrue(blogs.Length == 1);

			blog.Name = "Hammetts blog";
			blog.Save();

			blogs = Blog.FindAllByProperty("Name", null).ToArray();

			Assert.IsTrue(blogs.Length == 0);

			blogs = Blog.FindAllByProperty("Name", "Hammetts blog").ToArray();

			Assert.IsTrue(blogs.Length == 1);
		}
	}
}
