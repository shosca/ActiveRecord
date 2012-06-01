using Castle.ActiveRecord.Attributes;
using NHibernate.Event;

namespace Castle.ActiveRecord.Tests.Event {
	[EventListener]
	public class SamplePostUpdateListener : IPostUpdateEventListener
	{
		public void OnPostUpdate(PostUpdateEvent @event) { }
	}
}