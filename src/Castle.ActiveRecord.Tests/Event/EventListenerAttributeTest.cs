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
using Castle.ActiveRecord.Tests.Models;
using NHibernate.Event.Default;

namespace Castle.ActiveRecord.Tests.Event
{
	using System;
	using NUnit.Framework;
	using Castle.ActiveRecord;
	using Castle.ActiveRecord.Tests.Model;

	[TestFixture]
	public class EventListenerAttributeTest : AbstractEventListenerTest
	{
		[SetUp]
		public override void Init()
		{
			base.Init();
			ActiveRecord.Initialize(GetConfigSource());
		}

		[Test]
		public void C1_ListenerMustBeAddedWhenInitialized()
		{
			AssertListenerWasRegistered<SamplePreInsertListener, Blog>(e => e.PreInsertEventListeners);
		}

		[Test]
		public void C1_Listener_must_be_added_when_initialized_with_assembly()
		{
			AssertListenerWasRegistered<AttributedPreLoadListener, Blog>(e=>e.PreLoadEventListeners);
		}

		[Test]
		public void C5_Default_listener_is_present_in_configuration_after_adding_custom_listener()
		{
			AssertListenerWasRegistered<DefaultLoadEventListener, Blog>(e => e.LoadEventListeners);

			AssertListenerWasRegistered<AdditionalLoadListener, Blog>(e => e.LoadEventListeners);
		}

		[Test]
		public void U4_Default_listener_is_replaced_by_custom_listener_when_explicitly_demanded()
		{
			AssertListenerWasNotRegistered<DefaultLoadEventListener, OtherDbBlog>(e => e.LoadEventListeners);

			AssertListenerWasRegistered<ReplacementLoadListener, OtherDbBlog>(e => e.LoadEventListeners);
		}

		[Test]
		public void U4_All_specified_listeners_are_present_when_replacing_existing_listeners()
		{
			AssertListenerWasNotRegistered<DefaultLoadEventListener, OtherDbBlog>(e => e.LoadEventListeners);

			AssertListenerWasRegistered<ReplacementLoadListener, OtherDbBlog>(e => e.LoadEventListeners);
			AssertListenerWasRegistered<SecondAdditionalLoadListener, OtherDbBlog>(e => e.LoadEventListeners);
		}

		[Test]
		public void U3ab_One_listener_should_serve_multiple_events()
		{
			AssertListenerWasRegistered<MultipleListener, Blog>(e => e.PreLoadEventListeners);
			AssertListenerWasRegistered<MultipleListener, Blog>(e => e.PostLoadEventListeners);
		}

		[Test]
		public void U3c_Single_events_should_be_skipped()
		{
			AssertListenerWasNotRegistered<MultipleSkippedListener, Blog>(e => e.PreLoadEventListeners);
			AssertListenerWasRegistered<MultipleSkippedListener, Blog>(e => e.PostLoadEventListeners);
		}

		[Test]
		public void U3d_Multiple_events_are_served_by_different_instances_by_default()
		{
			Assert.AreNotSame(Array.Find(GetRegisteredListeners<Blog>(e => e.PreLoadEventListeners), l => (l is MultipleListener)),
			                  Array.Find(GetRegisteredListeners<Blog>(e => e.PostLoadEventListeners), l => (l is MultipleListener)));
		}

		[Test]
		public void U3d_Multiple_events_are_served_by_the_same_instance_when_singleton_is_defined()
		{
			Assert.AreSame(Array.Find(GetRegisteredListeners<Blog>(e => e.PreLoadEventListeners), l => (l is MultipleSingletonListener)),
							  Array.Find(GetRegisteredListeners<Blog>(e => e.PostLoadEventListeners), l => (l is MultipleSingletonListener)));
		}

        [Test]
        public void Event_listeners_are_registered_only_once()
        {
            Assert.AreEqual(1, Array.FindAll(GetRegisteredListeners<Blog>(e => e.PostInsertEventListeners), l => (l is SamplePostInsertListener)).Length);
            Assert.AreEqual(1, Array.FindAll(GetRegisteredListeners<Blog>(e => e.PostUpdateEventListeners), l => (l is SamplePostUpdateListener)).Length);
            Assert.AreEqual(1, Array.FindAll(GetRegisteredListeners<Blog>(e => e.PostDeleteEventListeners), l => (l is SamplePostDeleteListener)).Length);
        }

		[Test]
		public void U1_Multipledb_listeners_are_registered_correctly()
		{
			AssertListenerWasRegistered<FirstBaseListener, Blog>(e => e.PreLoadEventListeners);
			AssertListenerWasNotRegistered<SecondBaseListener, Blog>(e => e.PreLoadEventListeners);

			AssertListenerWasRegistered<SecondBaseListener, OtherDbBlog>(e => e.PreLoadEventListeners);
			AssertListenerWasNotRegistered<FirstBaseListener, OtherDbBlog>(e => e.PreLoadEventListeners);
		}
	}
}