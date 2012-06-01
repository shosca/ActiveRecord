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

namespace Castle.ActiveRecord.Tests.Event
{
	using NUnit.Framework;
	using NHibernate.Event;
	using System;
	using System.Collections.Generic;
	using Castle.ActiveRecord.Tests.Model;
	using NHibernate.Cfg;

	[TestFixture]
	public class EventListenerContributionTest : AbstractActiveRecordTest
	{
		[SetUp]
		public override void Init()
		{
			base.Init();
			ActiveRecord.ResetInitialization();
		}

		[Test]
		public void Listener_is_added_to_config() 
		{
			var contributor = new NHEventListeners();
			var listener = new MockListener();
			contributor.Add(listener);

			var source = GetConfigSource();
			source.GetConfiguration(string.Empty).AddContributor(contributor);
			ActiveRecord.Initialize(source);
			Recreate();

			Blog.FindAll();
			var listeners = ActiveRecord.Holder.GetConfiguration(typeof(Blog)).EventListeners.PostInsertEventListeners;
			Assert.Greater(Array.IndexOf(listeners, listener),-1);
		}
	
		[Test]
		public void Listener_is_called()
		{
			var contributor = new NHEventListeners();
			var listener = new MockListener();
			contributor.Add(listener);

			var source = GetConfigSource();
			source.GetConfiguration(string.Empty).AddContributor(contributor);
			ActiveRecord.Initialize(source);
			Recreate();

			using (new SessionScope()) {
				new Blog { Name = "Foo", Author = "Bar" }.Create();
			}

			Assert.IsTrue(IsCalled);
		}

		static bool IsCalled = false;

		public class MockListener : IPostInsertEventListener
		{

			#region IPostInsertEventListener Members

			public void OnPostInsert(PostInsertEvent @event)
			{
				IsCalled = true;
			}

			#endregion
		}

	}
}
