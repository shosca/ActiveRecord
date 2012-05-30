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

using System.Reflection;
using Castle.ActiveRecord.Scopes;
using Castle.ActiveRecord.Tests.Models;

namespace Castle.ActiveRecord.Tests.Conversation
{
    using System;
    using System.Linq;
    using NHibernate;
    using NUnit.Framework;

    [TestFixture]
    public class ConversationScenarioTest : NUnitInMemoryTest
    {
        public override Assembly[] GetAssemblies()
        {
            return new[] {typeof (Blog).Assembly};
        }

        [Test]
        public void BasicScenario()
        {
            // Arrange
            ArrangeRecords();

        	// Act
            IScopeConversation conversation = new ScopedConversation();
            Blog queriedBlog;
            using (new ConversationalScope(conversation))
            {
                queriedBlog = Blog.Find(1);
            }

            // No scope here
            Assert.That(queriedBlog.Posts, Is.Not.Empty);

            conversation.Dispose();
        }

    	[Test]
		public void CanCancelConversations()
		{
			ArrangeRecords();
    		using (var conversation = new ScopedConversation())
    		{
    			Blog blog = null;
    			using (new ConversationalScope(conversation))
    			{
    				blog = Blog.FindAll().First();
    			}

    			blog.Author = "Somebody else";

    			using (new ConversationalScope(conversation))
    			{
    				blog.SaveAndFlush();
    			}

    			conversation.Cancel();
    		}
			Assert.That(Blog.FindAll().First().Author, Is.EqualTo("Markus"));
		}

		[Test]
		public void CanSetFlushModeToNever()
		{
			ArrangeRecords();

			using (var conversation = new ScopedConversation(ConversationFlushMode.Explicit))
			{
				Blog blog;
				using (new ConversationalScope(conversation))
				{
					blog = Blog.FindAll().First();
					blog.Author = "Anonymous";
					blog.Save();
					Blog.FindAll(); // Triggers flushing if allowed
				}

				Assert.That(blog.Author, Is.EqualTo("Anonymous"));

				// Outside any ConversationalScope session-per-request is used
				Assert.That(Blog.FindAll().First().Author, Is.EqualTo("Markus"));

				conversation.Flush();
			}

			Assert.That(Blog.FindAll().First().Author, Is.EqualTo("Anonymous"));
		}

		[Test]
		public void CanSetFlushModeToOnClose()
		{
			ArrangeRecords();

			using (var conversation = new ScopedConversation(ConversationFlushMode.OnClose))
			{
				Blog blog;
				using (new ConversationalScope(conversation))
				{
					blog = Blog.FindAll().First();
					blog.Author = "Anonymous";
					blog.Save();
					Blog.FindAll(); // Triggers flushing if allowed
				}

				Assert.That(blog.Author, Is.EqualTo("Anonymous"));

				// Outside any ConversationalScope session-per-request is used
				Assert.That(Blog.FindAll().First().Author, Is.EqualTo("Markus"));

				// conversation.Flush(); // Only needed when set to explicit
			}

			Assert.That(Blog.FindAll().First().Author, Is.EqualTo("Anonymous"));
		}

    	[Test]
    	public void CanRestartAConversationWithFreshSessions()
    	{
    		ISession s1, s2;
    		using (var c = new ScopedConversation())
    		{
    			using (new ConversationalScope(c))
    			{
    				Blog.FindAll();
    				s1 = ActiveRecord.Holder.CreateSession(typeof (Blog));
    			}
				
				c.Restart();

				using (new ConversationalScope(c))
				{
					Blog.FindAll();
					s2 = ActiveRecord.Holder.CreateSession(typeof(Blog));
				}    			

				Assert.That(s1, Is.Not.SameAs(s2));
    			Assert.That(s1.IsOpen, Is.False);
    			Assert.That(s2.IsOpen, Is.True);
    		}
    	}

    	[Test]
    	public void CanUseIConversationDirectly()
    	{
    		ArrangeRecords();

    		using (IConversation conversation = new ScopedConversation())
    		{
    			Blog blog = null;
    			conversation.Execute(() => { blog = Blog.FindAll().First(); });

				Assert.That(blog, Is.Not.Null);
				Assume.That(blog.Author, Is.EqualTo("Markus"));

				// Lazy access
				Assert.That(blog.Posts.Count, Is.EqualTo(1));

    			blog.Author = "Anonymous";

    			conversation.Execute(() => blog.Save());
				
    		}

			Assert.That(Blog.FindAll().First().Author, Is.EqualTo("Anonymous"));
    	}



    	private void ArrangeRecords()
    	{
    		Blog blog = new Blog()
    		                	{
    		                		Author = "Markus",
    		                		Name = "Conversations"
    		                	};
    		Post post = new Post()
    		                	{
    		                		Blog = blog,
    		                		Category = "Scenario",
    		                		Title = "The Convesration is here",
    		                		Contents = "A new way for AR in fat clients",
    		                		Created = new DateTime(2010, 1, 1),
    		                		Published = true
    		                	};
    		blog.Save();
    		post.Save();
    	}
    }
}
