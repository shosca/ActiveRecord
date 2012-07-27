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


using System;

namespace Castle.ActiveRecord.Tests
{
	using System.Linq;
	using Castle.ActiveRecord;
	using Castle.ActiveRecord.Config;
	using Castle.ActiveRecord.Scopes;
	using Castle.ActiveRecord.Tests.Models;
	using NHibernate;
	using NUnit.Framework;

	[TestFixture]
	public class SessionScopeTestCase : AbstractActiveRecordTest
	{
		/*
		[Test]
		[ExpectedException(typeof(ActiveRecordException), ExpectedMessage = "A scope tried to registered itself within the framework, but the Active Record was not initialized")]
		public void GoodErrorMessageIfTryingToUseScopeWithoutInitializingFramework()
		{
			//Need to do this because other tests may have already initialized the framework.
			var scope = ThreadScopeAccessor.Instance.ScopeInfo;
			ThreadScopeAccessor.Instance.ScopeInfo = null;
			try
			{
				new SessionScope();
			}
			finally
			{
				ThreadScopeAccessor.Instance.ScopeInfo = scope;
			}
		}*/

		[Test, Ignore()]
		public void OneDatabaseSameSession()
		{
			// No scope here
			// So no optimization, thus different sessions

			var session1 = AR.Holder.CreateSession( typeof(Blog) );
			var session2 = AR.Holder.CreateSession( typeof(Blog) );

			Assert.IsNotNull( session1 );
			Assert.IsNotNull( session2 );
			Assert.IsTrue( session1 != session2 );

			AR.Holder.ReleaseSession(session1);
			AR.Holder.ReleaseSession(session2);

			// With scope

			using(new SessionScope())
			{
				session1 = AR.Holder.CreateSession( typeof(Blog) );
				session2 = AR.Holder.CreateSession( typeof(Post) );
				var session3 = AR.Holder.CreateSession( typeof(Blog) );
				var session4 = AR.Holder.CreateSession( typeof(Post) );

				Assert.IsNotNull( session1 );
				Assert.IsNotNull( session2 );
				Assert.IsNotNull( session3 );
				Assert.IsNotNull( session3 );

				Assert.IsTrue( session2 == session1 );
				Assert.IsTrue( session3 == session1 );
				Assert.IsTrue( session4 == session1 );

				AR.Holder.ReleaseSession(session1);
				AR.Holder.ReleaseSession(session2);
				AR.Holder.ReleaseSession(session3);
				AR.Holder.ReleaseSession(session4);

				session1 = AR.Holder.CreateSession( typeof(Post) );
				session2 = AR.Holder.CreateSession( typeof(Post) );
				session3 = AR.Holder.CreateSession( typeof(Blog) );
				session4 = AR.Holder.CreateSession( typeof(Blog) );

				Assert.IsNotNull( session1 );
				Assert.IsNotNull( session2 );
				Assert.IsNotNull( session3 );
				Assert.IsNotNull( session3 );

				Assert.IsTrue( session2 == session1 );
				Assert.IsTrue( session3 == session1 );
				Assert.IsTrue( session4 == session1 );
			}

			// Back to the old behavior

			session1 = AR.Holder.CreateSession( typeof(Blog) );
			session2 = AR.Holder.CreateSession( typeof(Blog) );

			Assert.IsNotNull( session1 );
			Assert.IsNotNull( session2 );
			Assert.IsTrue( session1 != session2 );

			AR.Holder.ReleaseSession(session1);
			AR.Holder.ReleaseSession(session2);
		}

		[Test]
		public void SessionScopeUsage()
		{
			Post.DeleteAll();
			Blog.DeleteAll();

			using(new SessionScope())
			{
				var blog = new Blog {Author = "hammett", Name = "some name"};
				blog.Save();

				var post = new Post(blog, "title", "post contents", "Castle");
				post.Save();
			}

			var blogs = Blog.FindAll().ToArray();
			Assert.AreEqual( 1, blogs.Length );

			var posts = Post.FindAll().ToArray();
			Assert.AreEqual( 1, posts.Length );
		}

		[Test]
		public void NestedSessionScopeUsage()
		{
			Post.DeleteAll();
			Blog.DeleteAll();

			using(new SessionScope())
			{
				Blog blog = new Blog();

				using(new SessionScope())
				{
					blog.Author = "hammett";
					blog.Name = "some name";
					blog.Save();
				}

				using(new SessionScope())
				{
					Post post = new Post(blog, "title", "post contents", "Castle");
					post.Save();
				}
			}

			Blog[] blogs = Blog.FindAll().ToArray();
			Assert.AreEqual( 1, blogs.Length );

			Post[] posts = Post.FindAll().ToArray();
			Assert.AreEqual( 1, posts.Length );
		}

		[Test]
		public void NestedSessionScopeAndLazyLoad()
		{
			var product = new Product();
			product.Categories.Add( new Category("x") );
			product.Categories.Add( new Category("y") );
			product.Categories.Add( new Category("z") ); 

			using (new SessionScope()) {
				product.Save();
			}

			using(new SessionScope())
			{
				var product1 = Product.Find(product.Id);
				Assert.AreEqual( 3, product1.Categories.Count );

				foreach(var cat in product1.Categories)
				{
					Assert.AreEqual(1, cat.Name.Length);
				}

				var product2 = Product.Find(product.Id);
				Assert.AreEqual( 3, product2.Categories.Count );

				using(new SessionScope())
				{
					foreach(var cat in product2.Categories)
					{
						Assert.AreEqual(1, cat.Name.Length);
					}
				}

				using(new SessionScope())
				{
					var product3 = Product.Find(product.Id);
					Assert.AreEqual( 3, product3.Categories.Count );

					foreach(var cat in product3.Categories)
					{
						Assert.AreEqual(1, cat.Name.Length);
					}
				}
			}
		}

		[Test]
		public void AnExceptionInvalidatesTheScopeAndPreventItsFlushing()
		{
			Post.DeleteAll();
			Blog.DeleteAll();

			Post post;

			// Prepare
			using(new SessionScope())
			{
				var blog = new Blog {Author = "hammett", Name = "some name"};
				blog.Save();

				post = new Post(blog, "title", "contents", "castle");
				post.Save();
			}

			using(var session = new SessionScope())
			{
				Assert.IsFalse(session.HasSessionError);

				Assert.Throws<ActiveRecordException>(() => {
					post = new Post(new Blog(100), "title", "contents", "castle");
					post.Save();
					session.Flush();
				});

				Assert.IsTrue(session.HasSessionError);
			}
		}

		[Test]
		public void SessionScopeFlushModeNever()
		{
			Post.DeleteAll();
			Blog.DeleteAll();

			using(new SessionScope(FlushAction.Never))
			{
				var blog = new Blog {Author = "hammett", Name = "some name"};

				//This gets flushed automatically because of the identity field
				blog.Save();

				var blogs = Blog.FindAll().ToArray();
				Assert.AreEqual(1, blogs.Length);

				//This change won't be saved to the db
				blog.Author = "A New Author";
				blog.Save();

				//The change should not be in the db
				blogs = Blog.FindAllByProperty("Author", "A New Author").ToArray();
				Assert.AreEqual(0, blogs.Length);
								
				SessionScope.Current.Flush();

				//The change should now be in the db
				blogs = Blog.FindAllByProperty("Author", "A New Author").ToArray();
				Assert.AreEqual(1, blogs.Length);

				//This change will be save to the db because it uses the SaveNow method
				blog.Name = "A New Name";
				blog.SaveAndFlush();

				//The change should now be in the db
				blogs = Blog.FindAllByProperty("Name", "A New Name").ToArray();
				Assert.AreEqual(1, blogs.Length);

				//This deletion should not get to the db
				blog.Delete();

				blogs = Blog.FindAll().ToArray();
				Assert.AreEqual(1, blogs.Length);
				
				SessionScope.Current.Flush();

				//The deletion should now be in the db
				blogs = Blog.FindAll().ToArray();
				Assert.AreEqual(0, blogs.Length);
			}
		}

		[Test]
		public void DifferentSessionScopesUseDifferentCaches()
		{
			Post.DeleteAll();
			Blog.DeleteAll();

			var blogId = 0;

			using (new SessionScope())
			{
				var blog = new Blog {Author = "MZY", Name = "FooBar"};
				blog.Save(); // Flushes due to IDENTITY
				blogId = blog.Id;
			}

			using (new SessionScope())
			{
				var blog = Blog.Find(blogId);
				blog.Name = "FooBarBaz";

				//Assert.AreEqual(FlushMode.Auto, blog.CurrentSession.FlushMode);
				//Assert.IsTrue(blog.CurrentSession.Transaction.IsActive);
				Assert.AreEqual(DefaultFlushType.Classic, AR.ConfigurationSource.DefaultFlushType);

				// Flushes automatically
				Assert.AreEqual(1, Blog.FindAllByProperty("Name", "FooBarBaz").Count());
			}

			using (new SessionScope())
			{
				var blog = Blog.Find(blogId);
				blog.Name = "FooBar";

				using (new SessionScope())
				{
					// Not flushed here
					Assert.AreEqual(0, Blog.FindAllByProperty("Name", "FooBar").Count());
				}
			}
			// Here it is flushed
			Assert.AreEqual(1, Blog.FindAllByProperty("Name", "FooBar").Count());
		}
	}
}
