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
        [Test]
        public void SessionsAreDifferent()
        {
            using (new SessionScope())
            {
                var blog = new Blog();
                var author = new Author();

                AR.FindAll<Blog>();
                AR.FindAll<Author>();

                Assert.AreNotSame(blog.GetCurrentSession(), author.GetCurrentSession());
                Assert.AreNotEqual(
                    blog.GetCurrentSession().Connection.ConnectionString,
                    author.GetCurrentSession().Connection.ConnectionString);
            }
        }

        [Test]
        public void OperateOne() {
            using (new SessionScope()) {
                var blogs = AR.FindAll<Blog>().ToArray();

                Assert.AreEqual(0, blogs.Length);

                CreateBlog();
            }

            using (new SessionScope()) {
                var blogs = AR.FindAll<Blog>().ToArray();
                Assert.AreEqual(1, blogs.Length);
            }
        }

        private static void CreateBlog()
        {
            var blog = new Blog {Name = "Senseless"};
            AR.Save(blog);
        }

        [Test]
        public void OperateTheOtherOne() {
            using (new SessionScope()) {
                var authors = AR.FindAll<Author>().ToArray();
                Assert.AreEqual(0, authors.Length);
                CreateAuthor();
            }

            using (var scope = new SessionScope()) {
                Assert.AreEqual(1, scope.Count<Author>());
            }
        }

        private static void CreateAuthor()
        {
            var author = new Author {Name = "Dr. Who"};
            AR.Save(author);
        }

        [Test]
        public void OperateBoth() {
            using (var scope = new SessionScope()) {
                var blogs = scope.FindAll<Blog>().ToArray();
                var authors = scope.FindAll<Author>().ToArray();

                Assert.AreEqual(0, blogs.Length);
                Assert.AreEqual(0, authors.Length);

                CreateBlog();
                CreateAuthor();
            }

            using (var scope = new SessionScope()) {
                Assert.AreEqual(1, scope.Count<Blog>());
                Assert.AreEqual(1, scope.Count<Author>());
            }
        }
    }
}
