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
		[Test]
		public void EventListenersAreRegisteredOnlyOnce() {
			Assert.AreEqual(1, Array.FindAll(GetRegisteredListeners<Blog>(e => e.PostInsertEventListeners), l => (l is SamplePostInsertListener)).Length);
			Assert.AreEqual(1, Array.FindAll(GetRegisteredListeners<Blog>(e => e.PostUpdateEventListeners), l => (l is SamplePostUpdateListener)).Length);
			Assert.AreEqual(1, Array.FindAll(GetRegisteredListeners<Blog>(e => e.PostDeleteEventListeners), l => (l is SamplePostDeleteListener)).Length);
		}

		[Test]
		public void ListenerMustBeAddedWhenInitialized()
		{
			AssertListenerWasRegistered<SamplePreInsertListener, Blog>(e => e.PreInsertEventListeners);
		}

		[Test]
		public void ListenerMustBeAddedWhenInitializedWithAssembly()
		{
			AssertListenerWasRegistered<AttributedPreLoadListener, Blog>(e=>e.PreLoadEventListeners);
		}

		[Test]
		public void DefaultListenerIsPresentInConfigurationAfterAddingCustomListener()
		{
			AssertListenerWasRegistered<DefaultLoadEventListener, Blog>(e => e.LoadEventListeners);

			AssertListenerWasRegistered<AdditionalLoadListener, Blog>(e => e.LoadEventListeners);
		}

		[Test]
		public void DefaultListenerIsReplacedByCustomListenerWhenExplicitlyDemanded()
		{
			AssertListenerWasNotRegistered<DefaultLoadEventListener, OtherDbBlog>(e => e.LoadEventListeners);

			AssertListenerWasRegistered<ReplacementLoadListener, OtherDbBlog>(e => e.LoadEventListeners);
		}

		[Test]
		public void AllSpecifiedListenersArePresentWhenReplacingExistingListeners()
		{
			AssertListenerWasNotRegistered<DefaultLoadEventListener, OtherDbBlog>(e => e.LoadEventListeners);

			AssertListenerWasRegistered<ReplacementLoadListener, OtherDbBlog>(e => e.LoadEventListeners);
			AssertListenerWasRegistered<SecondAdditionalLoadListener, OtherDbBlog>(e => e.LoadEventListeners);
		}

		[Test]
		public void OneListenerShouldServeMultipleEvents()
		{
			AssertListenerWasRegistered<MultipleListener, Blog>(e => e.PreLoadEventListeners);
			AssertListenerWasRegistered<MultipleListener, Blog>(e => e.PostLoadEventListeners);
		}

		[Test]
		public void SingleEventsShouldBeSkipped()
		{
			AssertListenerWasNotRegistered<MultipleSkippedListener, Blog>(e => e.PreLoadEventListeners);
			AssertListenerWasRegistered<MultipleSkippedListener, Blog>(e => e.PostLoadEventListeners);
		}

		[Test]
		public void MultipleEventsAreServedByDifferentInstancesByDefault()
		{
			Assert.AreNotSame(Array.Find(GetRegisteredListeners<Blog>(e => e.PreLoadEventListeners), l => (l is MultipleListener)),
			                  Array.Find(GetRegisteredListeners<Blog>(e => e.PostLoadEventListeners), l => (l is MultipleListener)));
		}

		[Test]
		public void MultipleEventsAreServedByTheSameInstanceWhenSingletonIsDefined()
		{
			Assert.AreSame(Array.Find(GetRegisteredListeners<Blog>(e => e.PreLoadEventListeners), l => (l is MultipleSingletonListener)),
							  Array.Find(GetRegisteredListeners<Blog>(e => e.PostLoadEventListeners), l => (l is MultipleSingletonListener)));
		}

		[Test]
		public void MultipledbListenersAreRegisteredCorrectly()
		{
			AssertListenerWasRegistered<FirstBaseListener, Blog>(e => e.PreLoadEventListeners);
			AssertListenerWasNotRegistered<SecondBaseListener, Blog>(e => e.PreLoadEventListeners);

			AssertListenerWasRegistered<SecondBaseListener, OtherDbBlog>(e => e.PreLoadEventListeners);
			AssertListenerWasNotRegistered<FirstBaseListener, OtherDbBlog>(e => e.PreLoadEventListeners);
		}
	}
}
