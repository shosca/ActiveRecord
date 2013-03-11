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

namespace Castle.ActiveRecord.Tests
{
    using System;
    using NUnit.Framework;
    using NHibernate;

    [TestFixture]
    public class TransactionScopeTestCase : AbstractActiveRecordTest
    {
        [Test]
        public void TransactionScopeUsage()
        {
            using(var scope = new TransactionScope())
            {
                var session1 = scope.CreateSession<Blog>();
                var session2 = scope.CreateSession<Post>();
                var session3 = scope.CreateSession<Blog>();
                var session4 = scope.CreateSession<Post>();

                Assert.IsNotNull(session1);
                Assert.IsNotNull(session2);
                Assert.IsNotNull(session3);
                Assert.IsNotNull(session3);

                Assert.IsTrue(session2 == session1);
                Assert.IsTrue(session3 == session1);
                Assert.IsTrue(session4 == session1);
            }
        }

        [Test]
        public void RollbackVote()
        {
            using(var transaction = new TransactionScope())
            {
                var blog = new Blog {Author = "hammett", Name = "some name"};
                blog.Save();

                var post = new Post(blog, "title", "post contents", "Castle");
                post.Save();

                // pretend something went wrong

                transaction.VoteRollBack();
            }

            using (new SessionScope()) {
                var blogs = Blog.FindAll().ToArray();
                Assert.AreEqual(0, blogs.Length);

                var posts = Post.FindAll().ToArray();
                Assert.AreEqual(0, posts.Length);
            }
        }

        [Test]
        public void RollbackOnDispose()
        {
            using(new TransactionScope(ondispose: OnDispose.Rollback))
            {
                var blog = new Blog {Author = "hammett", Name = "some name"};
                blog.Save();

                var post = new Post(blog, "title", "post contents", "Castle");
                post.Save();
            }

            using (new SessionScope()) {
                var blogs = Blog.FindAll().ToArray();
                Assert.AreEqual(0, blogs.Length);

                var posts = Post.FindAll().ToArray();
                Assert.AreEqual(0, posts.Length);
            }
        }

        [Test]
        public void CommitVote()
        {
            using(new TransactionScope())
            {
                var blog = new Blog {Author = "hammett", Name = "some name"};
                blog.Save();

                var post = new Post(blog, "title", "post contents", "Castle");
                post.Save();

                // Default to VoteCommit
            }

            using (new SessionScope()) {
                var blogs = Blog.FindAll().ToArray();
                Assert.AreEqual(1, blogs.Length);

                var posts = Post.FindAll().ToArray();
                Assert.AreEqual(1, posts.Length);
            }
        }

        [Test]
        public void RollbackUponException()
        {
            using(var transaction = new TransactionScope())
            {
                var blog = new Blog {Author = "hammett", Name = "some name"};
                blog.Save();

                var post = new Post(blog, "title", "post contents", "Castle");

                try
                {
                    post.SaveWithException();
                }
                catch(Exception)
                {
                    transaction.VoteRollBack();
                }
            }

            using (new SessionScope()) {
                var blogs = Blog.FindAll().ToArray();
                Assert.AreEqual(0, blogs.Length);

                var posts = Post.FindAll().ToArray();
                Assert.AreEqual(0, posts.Length);
            }
        }

        [Test]
        public void NestedTransactions()
        {
            using(new TransactionScope())
            {
                var blog = new Blog();

                using(var t1 = new TransactionScope(TransactionMode.Inherits))
                {
                    blog.Author = "hammett";
                    blog.Name = "some name";
                    blog.Save();

                    t1.VoteCommit();
                }

                using(var t2 = new TransactionScope(TransactionMode.Inherits))
                {
                    var post = new Post(blog, "title", "post contents", "Castle");

                    try
                    {
                        post.SaveWithException();
                    }
                    catch(Exception)
                    {
                        t2.VoteRollBack();
                    }
                }
            }

            using (new SessionScope()) {
                var blogs = Blog.FindAll().ToArray();
                Assert.AreEqual(0, blogs.Length);

                var posts = Post.FindAll().ToArray();
                Assert.AreEqual(0, posts.Length);
            }
        }

        [Test]
        public void NestedTransactionScopesHaveCorrectTransactionContexts()
        {
            using (new TransactionScope())
            {
                var blog1 = new Blog();
                Blog.FindAll();

                var s1 = blog1.GetCurrentSession();
                var tx1 = s1.Transaction;
                Assert.IsNotNull(tx1);

                using (new TransactionScope())
                {
                    var blog2 = new Blog();
                    Blog.FindAll();
                    var s2 = blog2.GetCurrentSession();
                    var tx2 = s2.Transaction;

                    Assert.IsNotNull(tx2);
                    Assert.AreNotSame(tx1, tx2);
                    
                    // TransactionScope uses a new session!
                    // Assert.AreSame(s1, s2);
                }

                using (new TransactionScope(TransactionMode.Inherits))
                {
                    var blog3 = new Blog();
                    Blog.FindAll();
                    var tx3 = blog3.GetCurrentSession().Transaction;

                    Assert.IsNotNull(tx3);
                    Assert.AreSame(tx1, tx3);
                }

                Assert.IsTrue(tx1.IsActive);
            }

            using (new SessionScope())
            {
                var blog4 = new Blog();
                Blog.FindAll();

                using (new TransactionScope())
                {
                    var blog5 = new Blog();

                    Assert.AreSame(blog4.GetCurrentSession().Transaction, blog5.GetCurrentSession().Transaction);
                }
            }

            using (new SessionScope())
            {
                var blog6 = new Blog();
                var session = blog6.GetCurrentSession();

                Assert.IsNotNull(session.Transaction);
                var tx4 = session.Transaction;
                using (var tx5 = session.BeginTransaction())
                {
                    Assert.AreSame(tx4, tx5);
                    Blog.FindAll();

                    using (var tx6 = session.BeginTransaction())
                    {
                        Assert.AreSame(tx5, tx6);
                    }
                }
            }
        }

        [Test]
        public void LotsOfNestedTransactionWithDifferentConfigurations()
        {
            using(var root = new TransactionScope())
            {
                using(var t1 = new TransactionScope()) // Isolated
                {
                    var blog = new Blog();

                    Blog.FindAll(); // Just to force a session association

                    using(new TransactionScope(TransactionMode.Inherits))
                    {
                        Blog.FindAll(); // Just to force a session association

                        blog.Author = "hammett";
                        blog.Name = "some name";
                        blog.Save();
                    }

                    using(new TransactionScope(TransactionMode.Inherits))
                    {
                        var post = new Post(blog, "title", "post contents", "Castle");

                        post.Save();
                    }

                    t1.VoteRollBack();
                }

                Blog.FindAll(); // Cant be locked

                using(new TransactionScope())
                {
                    var blog = new Blog();
                    Blog.FindAll(); // Just to force a session association

                    using(new TransactionScope())
                    {
                        Blog.FindAll(); // Just to force a session association

                        blog.Author = "hammett";
                        blog.Name = "some name";
                        blog.Save();
                    }

                    using(var t1n = new TransactionScope(TransactionMode.Inherits))
                    {
                        var post = new Post(blog, "title", "post contents", "Castle");

                        try
                        {
                            post.SaveWithException();
                        }
                        catch(Exception)
                        {
                            t1n.VoteRollBack();
                        }
                    }
                }

                root.VoteCommit();
            }

            using (new SessionScope()) {
                var blogs = Blog.FindAll().ToArray();
                Assert.AreEqual(1, blogs.Length);

                var posts = Post.FindAll().ToArray();
                Assert.AreEqual(0, posts.Length);
            }
        }

        [Test]
        public void MixingSessionScopeAndTransactionScopes()
        {
            using(new SessionScope())
            {
                using(var root = new TransactionScope())
                {
                    using(var t1 = new TransactionScope()) // Isolated
                    {
                        var blog = new Blog();

                        Blog.FindAll(); // Just to force a session association

                        using(new TransactionScope(TransactionMode.Inherits))
                        {
                            Blog.FindAll(); // Just to force a session association

                            blog.Author = "hammett";
                            blog.Name = "some name";
                            blog.Save();
                        }

                        using(new TransactionScope(TransactionMode.Inherits))
                        {
                            var post = new Post(blog, "title", "post contents", "Castle");

                            post.Save();
                        }

                        t1.VoteRollBack();
                    }

                    Blog.FindAll(); // Cant be locked

                    using(new TransactionScope())
                    {
                        var blog = new Blog();
                        Blog.FindAll(); // Just to force a session association

                        using(new TransactionScope())
                        {
                            Blog.FindAll(); // Just to force a session association

                            blog.Author = "hammett";
                            blog.Name = "some name";
                            blog.Save();
                        }

                        using(var t1 = new TransactionScope(TransactionMode.Inherits))
                        {
                            var post = new Post(blog, "title", "post contents", "Castle");

                            try
                            {
                                post.SaveWithException();
                            }
                            catch(Exception)
                            {
                                t1.VoteRollBack();
                            }
                        }
                    }

                    root.VoteCommit();
                }
            }

            using (new SessionScope()) {
                var blogs = Blog.FindAll().ToArray();
                Assert.AreEqual(1, blogs.Length);

                var posts = Post.FindAll().ToArray();
                Assert.AreEqual(0, posts.Length);
            }
        }

        [Test]
        public void MixingSessionScopeAndTransactionScopes2()
        {
            var b = new Blog();

            using(new SessionScope())
            {
                b.Name = "a";
                b.Author = "x";
                b.Save();

                using(new TransactionScope())
                {
                    for(var i = 1; i <= 10; i++)
                    {
                        var post = new Post(b, "t", "c", "General");
                        post.Save();
                    }
                }
            }

            using(new SessionScope())
            {
                // We should load this outside the transaction scope

                b = Blog.Find(b.Id);

                using(new TransactionScope())
                {
                    if (b.Posts.Count > 0) {
                        foreach(var p in b.Posts)
                        {
                            p.Delete();
                        }
                    }

                    b.Delete();
                }
            }

            using (new SessionScope()) {
                var blogs = Blog.FindAll().ToArray();
                Assert.AreEqual(0, blogs.Length);

                var posts = Post.FindAll().ToArray();
                Assert.AreEqual(0, posts.Length);
            }
        }

        [Test]
        public void MixingSessionScopeAndTransactionScopes3()
        {
            var b = new Blog();

            using(new SessionScope())
            {
                b.Name = "a";
                b.Author = "x";
                b.Save();

                using(new TransactionScope())
                {
                    for(var i = 1; i <= 10; i++)
                    {
                        var post = new Post(b, "t", "c", "General");
                        post.Save();
                    }
                }
            }

            using(new SessionScope())
            {
                // We should load this outside the transaction scope

                b = Blog.Find(b.Id);

                using(var transaction = new TransactionScope())
                {
                    if (b.Posts.Count > 0)
                        foreach(var p in b.Posts)
                        {
                            p.Delete();
                        }

                    b.Delete();

                    transaction.VoteRollBack();
                }
            }

            using (new SessionScope()) {
                var blogs = Blog.FindAll().ToArray();
                Assert.AreEqual(1, blogs.Length);

                var posts = Post.FindAll().ToArray();
                Assert.AreEqual(10, posts.Length);
            }
        }

        [Test]
        public void MixingSessionScopeAndTransactionScopes4()
        {
            var b = new Blog();
            Post post = null;

            using(new SessionScope()) {
                b.Name = "a";
                b.Author = "x";
                b.Save();

                post = new Post(b, "t", "c", "General");
                post.Save();
            }

            using(new SessionScope())
            {
                using(new TransactionScope(TransactionMode.Inherits))
                {
                    b = Blog.Find(b.Id);
                    b.Name = "changed";
                    b.Save();
                }

                Post.Find(post.Id);
                Blog.Find(b.Id);

                using(new TransactionScope(TransactionMode.Inherits))
                {
                    Post.Find(post.Id);
                    Blog.Find(b.Id);
                }
            }

            using (new SessionScope()) {
                var blogs = Blog.FindAll().ToArray();
                Assert.AreEqual(1, blogs.Length);

                var posts = Post.FindAll().ToArray();
                Assert.AreEqual(1, posts.Length);
            }
        }

        [Test]
        public void NestedTransactionWithRollbackOnDispose()
        {
            using(new TransactionScope())
            {
                var blog = new Blog();

                using(var t1 = new TransactionScope(mode: TransactionMode.Inherits, ondispose: OnDispose.Rollback))
                {
                    blog.Author = "hammett";
                    blog.Name = "some name";
                    blog.Save();

                    t1.VoteCommit();
                }

                using(var t2 = new TransactionScope(TransactionMode.Inherits, ondispose: OnDispose.Rollback))
                {
                    var post = new Post(blog, "title", "post contents", "Castle");

                    try
                    {
                        post.SaveWithException();

                        t2.VoteCommit(); // Will never be called
                    }
                    catch(Exception)
                    {
                        // t2.VoteRollBack();
                    }
                }
            }

            using (new SessionScope()) {
                var blogs = Blog.FindAll().ToArray();
                Assert.AreEqual(0, blogs.Length);

                var posts = Post.FindAll().ToArray();
                Assert.AreEqual(0, posts.Length);
            }
        }

        [Test]
        public void ReportedProblemOnForum()
        {
            using(new TransactionScope())
            {
                var comp1 = new Company("comp1");
                comp1.Create();

                var comp2 = new Company("comp2");
                comp2.Create();
            }
        }

        [Test]
        public void ExplicitFlushInsideSecondTransactionProblem()
        {
            var comp1 = new Company("comp1");
            var comp2 = new Company("comp2");
            using(new SessionScope())
            {
                comp1.Create();
                comp2.Create();
            }

            using(new SessionScope(FlushAction.Never))
            {
                using(var tx = new TransactionScope(ondispose: OnDispose.Rollback))
                {
                    var comp2a = Company.Find(comp2.Id);
                    comp2a.Name = "changed";
                    tx.VoteCommit();
                }

                using(var scope = new TransactionScope(ondispose: OnDispose.Rollback))
                {
                    var changedCompanies = AR.FindAllByProperty<Company>("Name", "changed");
                    Assert.AreEqual(1, changedCompanies.Count());
                    var e2a = changedCompanies.First();
                    e2a.Delete();

                    scope.Flush();

                    Assert.AreEqual(0, AR.FindAllByProperty<Company>("Name", "changed").Count());
                }
            }
        }
    }
}
