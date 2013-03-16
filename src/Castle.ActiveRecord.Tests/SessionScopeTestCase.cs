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
        [Test]
        public void TwoSessionsOneScope()
        {
            using (var scope1 = new SessionScope())
            using (var scope2 = new SessionScope()) {
                var session1 = scope1.OpenSession<Blog>();
                var session2 = scope2.OpenSession<Blog>();
                Assert.IsNotNull( session1 );
                Assert.IsNotNull( session2 );
                Assert.IsTrue( session1 == session2 ); // will use parent scope's sessions
            }

            using (var scope = new SessionScope()) {
                var session1 = scope.OpenSession<Blog>();
                var session2 = scope.OpenSession<Blog>();
                Assert.IsNotNull( session1 );
                Assert.IsNotNull( session2 );
                Assert.IsTrue( session1 == session2 );
            }
        }

        [Test]
        public void SessionScopeUsage()
        {
            using (new SessionScope()) {
                Post.DeleteAll();
                Blog.DeleteAll();
            }

            using(new SessionScope())
            {
                var blog = new Blog {Author = "hammett", Name = "some name"};
                blog.Save();

                var post = new Post(blog, "title", "post contents", "Castle");
                post.Save();
            }

            using (new SessionScope()) {
                var blogs = Blog.FindAll().ToArray();
                Assert.AreEqual( 1, blogs.Length );

                var posts = Post.FindAll().ToArray();
                Assert.AreEqual( 1, posts.Length );
            }
        }

        [Test]
        public void NestedSessionScopeUsage()
        {
            using (new SessionScope()) {
                Post.DeleteAll();
                Blog.DeleteAll();
            }

            using(new SessionScope())
            {
                var blog = new Blog();

                using(new SessionScope())
                {
                    blog.Author = "hammett";
                    blog.Name = "some name";
                    blog.Save();
                }

                using(new SessionScope())
                {
                    var post = new Post(blog, "title", "post contents", "Castle");
                    post.Save();
                }
            }

            using (new SessionScope()) {
                var blogs = Blog.FindAll().ToArray();
                Assert.AreEqual( 1, blogs.Length );

                var posts = Post.FindAll().ToArray();
                Assert.AreEqual( 1, posts.Length );
            }
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
            using (new SessionScope()) {
                Post.DeleteAll();
                Blog.DeleteAll();
            }

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
            using (new SessionScope()) {
                Post.DeleteAll();
                Blog.DeleteAll();
            }

            using(var scope = new SessionScope(FlushAction.Never))
            {
                var blog = new Blog {Author = "hammett", Name = "some name"};

                //This gets flushed automatically because of the identity field
                blog.Save();

                var blogs = Blog.FindAll().ToArray();
                Assert.AreEqual(1, blogs.Length);

                //This change won't be saved to the db
                blog.Author = "A New Author";
                blog.Save(false);

                //The change should not be in the db
                blogs = Blog.FindAllByProperty("Author", "A New Author").ToArray();
                Assert.AreEqual(0, blogs.Length);
                                
                scope.Flush();

                //The change should now be in the db
                blogs = Blog.FindAllByProperty("Author", "A New Author").ToArray();
                Assert.AreEqual(1, blogs.Length);

                //This change will be save to the db
                blog.Name = "A New Name";
                blog.Save();

                //The change should now be in the db
                blogs = Blog.FindAllByProperty("Name", "A New Name").ToArray();
                Assert.AreEqual(1, blogs.Length);

                //This deletion should not get to the db
                blog.Delete(false);

                blogs = Blog.FindAll().ToArray();
                Assert.AreEqual(1, blogs.Length);

                scope.Flush();

                //The deletion should now be in the db
                blogs = Blog.FindAll().ToArray();
                Assert.AreEqual(0, blogs.Length);
            }
        }

        [Test]
        public void DifferentSessionScopesUseDifferentCaches()
        {
            using (new SessionScope()) {
                Post.DeleteAll();
                Blog.DeleteAll();
            }

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
                Assert.AreEqual(DefaultFlushType.Auto, AR.Holder.ConfigurationSource.DefaultFlushType);

                // Flushes automatically
                Assert.AreEqual(1, Blog.FindAllByProperty("Name", "FooBarBaz").Count());
            }

            using (new SessionScope())
            {
                var blog = Blog.Find(blogId);
                blog.Name = "FooBar";

                using (new SessionScope())
                {
                    // Will use parent's sessions
                    Assert.AreEqual(1, Blog.FindAllByProperty("Name", "FooBar").Count());
                }
            }
            // Here it is flushed
            using (new SessionScope())
                Assert.AreEqual(1, Blog.FindAllByProperty("Name", "FooBar").Count());
        }
    }
}
