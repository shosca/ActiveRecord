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


namespace Castle.ActiveRecord.Tests
{
    using System;
    using System.Data.SqlClient;
    using System.Linq;
    
    using Castle.Core.Configuration;
    using Castle.ActiveRecord.Config;
    using Castle.ActiveRecord.Scopes;
    using Castle.ActiveRecord.Tests.Model;
    using Castle.ActiveRecord.Tests.Models;
    
    using NHibernate;
    
    using NUnit.Framework;

    [TestFixture]
    public class DifferentDatabaseScopeTestCase : AbstractActiveRecordTest
    {
        [Test]
        public void SimpleCase()
        {
            var blog = new Blog {Name = "hammett's blog", Author = "hamilton verissimo"};

            using (new SessionScope()) {
                blog.Save();
            }

            using (var conn = CreateSqlConnection2())
            {
                conn.Open();

                using(new DifferentDatabaseScope(conn))
                {
                    blog.Create();
                }
            }

            using (new SessionScope()) {
                var blogs = Blog.FindAll().ToArray();
                Assert.IsNotNull( blogs );
                Assert.AreEqual( 1, Blog.Count());

                var blogs2 = OtherDbBlog.FindAll().ToArray();
                Assert.IsNotNull( blogs2 );
                Assert.AreEqual( 1, OtherDbBlog.Count());
            }
        }

        [Test]
        public void UsingSessionScope()
        {
            ISession session1, session2;

            using(new SessionScope())
            {
                Blog blog = new Blog {Name = "hammett's blog", Author = "hamilton verissimo"};
                blog.Save();

                session1 = blog.GetCurrentSession();
                Assert.IsNotNull(session1);
                blog.Evict();

                SqlConnection conn = CreateSqlConnection2();

                using(conn)
                {
                    conn.Open();

                    using(new DifferentDatabaseScope(conn))
                    {
                        blog.Create();

                        session2 = blog.GetCurrentSession();
                        Assert.IsNotNull(session2);

                        Assert.IsFalse( Object.ReferenceEquals(session1, session2) );
                    }
                }
            }

            using (new SessionScope()) {
                var blogs = Blog.FindAll().ToArray();
                Assert.IsNotNull( blogs );
                Assert.AreEqual(1, Blog.Count());

                var blogs2 = OtherDbBlog.FindAll().ToArray();
                Assert.IsNotNull( blogs2 );
                Assert.AreEqual(1, OtherDbBlog.Count());
            }
        }

        [Test]
        public void UsingTransactionScope()
        {
            SqlConnection conn = CreateSqlConnection2();
            ISession session1, session2;
            ITransaction trans1, trans2;

            using(new TransactionScope())
            {
                Blog blog = new Blog {Name = "hammett's blog", Author = "hamilton verissimo"};
                blog.Save();
                blog.Evict();
                
                session1 = blog.GetCurrentSession();
                trans1 = blog.GetCurrentSession().Transaction;

                Assert.IsNotNull(session1);
                Assert.IsNotNull(session1.Transaction);
                Assert.IsFalse(session1.Transaction.WasCommitted);
                Assert.IsFalse(session1.Transaction.WasRolledBack);

                conn.Open();

                using(new DifferentDatabaseScope(conn))
                {
                    blog.Create();

                    session2 = blog.GetCurrentSession();
                    trans2 = blog.GetCurrentSession().Transaction;
                    Assert.IsNotNull(session2);

                    Assert.IsFalse( Object.ReferenceEquals(session1, session2) );

                    Assert.IsNotNull(session2.Transaction);
                    Assert.IsFalse(session2.Transaction.WasCommitted);
                    Assert.IsFalse(session2.Transaction.WasRolledBack);
                }
            }

            Assert.IsFalse(session1.IsOpen);
            Assert.IsFalse(session2.IsOpen);
            Assert.IsTrue(trans1.WasCommitted);
            Assert.IsFalse(trans1.WasRolledBack);
            Assert.IsTrue(trans2.WasCommitted);
            Assert.IsFalse(trans2.WasRolledBack);

            using (new SessionScope()) {
                var blogs = Blog.FindAll().ToArray();
                Assert.IsNotNull( blogs );
                Assert.AreEqual( 1, Blog.Count());

                var blogs2 = OtherDbBlog.FindAll().ToArray();
                Assert.IsNotNull( blogs2 );
                Assert.AreEqual( 1, OtherDbBlog.Count());
            }
        }

        [Test]
        public void UsingTransactionScopeWithRollback()
        {
            SqlConnection conn = CreateSqlConnection2();
            ISession session1, session2;
            ITransaction trans1, trans2;

            using(TransactionScope scope = new TransactionScope())
            {
                Blog blog = new Blog {Name = "hammett's blog", Author = "hamilton verissimo"};
                blog.Save();
                
                session1 = blog.GetCurrentSession();
                trans1 = blog.GetCurrentSession().Transaction;
                Assert.IsNotNull(session1);
                Assert.IsNotNull(session1.Transaction);
                Assert.IsFalse(session1.Transaction.WasCommitted);
                Assert.IsFalse(session1.Transaction.WasRolledBack);

                blog.Evict();
                conn.Open();

                using(new DifferentDatabaseScope(conn))
                {
                    blog.Create();

                    session2 = blog.GetCurrentSession();
                    trans2 = blog.GetCurrentSession().Transaction;
                    Assert.IsNotNull(session2);

                    Assert.IsFalse( Object.ReferenceEquals(session1, session2) );

                    Assert.IsNotNull(session2.Transaction);
                    Assert.IsFalse(session2.Transaction.WasCommitted);
                    Assert.IsFalse(session2.Transaction.WasRolledBack);

                    scope.VoteRollBack();
                }
            }

            Assert.IsFalse(session1.IsOpen);
            Assert.IsFalse(session2.IsOpen);
            Assert.IsFalse(trans1.WasCommitted);
            Assert.IsTrue(trans1.WasRolledBack);
            Assert.IsFalse(trans2.WasCommitted);
            Assert.IsTrue(trans2.WasRolledBack);

            using (new SessionScope()) {
                var blogs = Blog.FindAll().ToArray();
                Assert.IsNotNull( blogs );
                Assert.AreEqual(0, Blog.Count());

                var blogs2 = OtherDbBlog.FindAll().ToArray();
                Assert.IsNotNull( blogs2 );
                Assert.AreEqual(0, OtherDbBlog.Count());
            }
        }

        [Test]
        public void MoreThanOneConnectionWithinTheSessionScope()
        {
            var conn = CreateSqlConnection();
            var conn2 = CreateSqlConnection2();

            using(new SessionScope())
            {
                foreach(var connection in new [] { conn, conn2 })
                {
                    connection.Open();

                    using(new DifferentDatabaseScope(connection))
                    {
                        var blog = new Blog {Name = "hammett's blog", Author = "hamilton verissimo"};
                        blog.Create();
                    }
                }
            }

            using (new SessionScope()) {
                var blogs2 = OtherDbBlog.FindAll().ToArray();
                Assert.IsNotNull( blogs2 );
                Assert.AreEqual( 1, blogs2.Length );

                var blogs = Blog.FindAll().ToArray();
                Assert.IsNotNull( blogs );
                Assert.AreEqual( 1, blogs.Length );
            }
        }

        [Test]
        public void UsingSessionAndTransactionScope()
        {
            SqlConnection conn = CreateSqlConnection2();
            ISession session1, session2;

            ITransaction trans1, trans2;
            using(new SessionScope())
            {
                using(new TransactionScope())
                {
                    var blog = new Blog {Name = "hammett's blog", Author = "hamilton verissimo"};
                    blog.Save();
                    
                    session1 = blog.GetCurrentSession();
                    Assert.IsNotNull(session1);
                    trans1 = session1.Transaction;
                    Assert.IsNotNull(trans1);
                    Assert.IsFalse(trans1.WasCommitted);
                    Assert.IsFalse(trans1.WasRolledBack);

                    conn.Open();

                    using(new DifferentDatabaseScope(conn))
                    {
                        blog = new Blog {Name = "hammett's blog", Author = "hamilton verissimo"};
                        blog.Save();

                        session2 = blog.GetCurrentSession();
                        Assert.IsNotNull(session2);
                        trans2 = session2.Transaction;
                        Assert.IsFalse( Object.ReferenceEquals(session1, session2) );

                        Assert.IsNotNull(session2.Transaction);
                        Assert.IsFalse(session2.Transaction.WasCommitted);
                        Assert.IsFalse(session2.Transaction.WasRolledBack);
                    }

                    using(new DifferentDatabaseScope(conn))
                    {
                        blog = new Blog {Name = "hammett's blog", Author = "hamilton verissimo"};
                        blog.Save();

                        session2 = blog.GetCurrentSession();
                        Assert.IsNotNull(session2);

                        Assert.IsFalse( Object.ReferenceEquals(session1, session2) );

                        Assert.IsNotNull(session2.Transaction);
                        Assert.IsFalse(session2.Transaction.WasCommitted);
                        Assert.IsFalse(session2.Transaction.WasRolledBack);
                    }
                }
            }

            Assert.IsFalse(session1.IsOpen);
            Assert.IsFalse(session2.IsOpen);
            Assert.IsTrue(trans1.WasCommitted);
            Assert.IsFalse(trans1.WasRolledBack);
            Assert.IsTrue(trans2.WasCommitted);
            Assert.IsFalse(session2.Transaction.WasRolledBack);

            using (new SessionScope()) {
                var blogs = Blog.FindAll().ToArray();
                Assert.IsNotNull(blogs);
                Assert.AreEqual(1, Blog.Count());

                var blogs2 = OtherDbBlog.FindAll().ToArray();
                Assert.IsNotNull(blogs2);
                Assert.AreEqual(2, OtherDbBlog.Count());
            }
        }

        [Test]
        public void SequenceOfTransactions()
        {
            var conn = CreateSqlConnection2();
            ISession session1, session2;

            ITransaction trans1, trans2;
            using(new SessionScope())
            {
                using(new TransactionScope())
                {
                    var blog = new Blog {Name = "hammett's blog", Author = "hamilton verissimo"};
                    blog.Save();
                    
                    session1 = blog.GetCurrentSession();
                    Assert.IsNotNull(session1);
                    trans1 = session1.Transaction;
                    Assert.IsNotNull(trans1);
                    Assert.IsFalse(trans1.WasCommitted);
                    Assert.IsFalse(trans1.WasRolledBack);

                    conn.Open();

                    using(new DifferentDatabaseScope(conn))
                    {
                        blog = new Blog {Name = "hammett's blog", Author = "hamilton verissimo"};
                        blog.Save();

                        session2 = blog.GetCurrentSession();
                        Assert.IsNotNull(session2);
                        trans2 = session2.Transaction;
                        Assert.IsFalse( Object.ReferenceEquals(session1, session2) );

                        Assert.IsNotNull(session2.Transaction);
                        Assert.IsFalse(session2.Transaction.WasCommitted);
                        Assert.IsFalse(session2.Transaction.WasRolledBack);
                    }

                    using(new DifferentDatabaseScope(conn))
                    {
                        blog = new Blog {Name = "hammett's blog", Author = "hamilton verissimo"};
                        blog.Save();

                        session2 = blog.GetCurrentSession();
                        Assert.IsNotNull(session2);

                        Assert.IsFalse( Object.ReferenceEquals(session1, session2) );

                        Assert.IsNotNull(trans2);
                        Assert.IsFalse(trans2.WasCommitted);
                        Assert.IsFalse(trans2.WasRolledBack);
                    }
                }

                conn.Close();

                using(new TransactionScope())
                {
                    var blog = new Blog {Name = "another blog", Author = "erico verissimo"};
                    blog.Save();
                }
            }

            Assert.IsFalse(session1.IsOpen);
            Assert.IsFalse(session2.IsOpen);
            Assert.IsTrue(trans1.WasCommitted);
            Assert.IsFalse(trans1.WasRolledBack);
            Assert.IsTrue(trans2.WasCommitted);
            Assert.IsFalse(trans2.WasRolledBack);

            using (new SessionScope()) {
                var blogs = Blog.FindAll().ToArray();
                Assert.IsNotNull(blogs);
                Assert.AreEqual(2, Blog.Count());

                var blogs2 = OtherDbBlog.FindAll().ToArray();
                Assert.IsNotNull(blogs2);
                Assert.AreEqual(2, OtherDbBlog.Count());
            }
        }

        private SqlConnection CreateSqlConnection()
        {
            IActiveRecordConfiguration config = GetConfigSource();
    
            var db2 = config.GetConfiguration(string.Empty);
    
            return new SqlConnection(db2.Properties["connection.connection_string"]);
        }

        private SqlConnection CreateSqlConnection2()
        {
            IActiveRecordConfiguration config = GetConfigSource();

            var db2 = config.GetConfiguration("Test2ARBase");

            return new SqlConnection(db2.Properties["connection.connection_string"]);
        }
    }
}
