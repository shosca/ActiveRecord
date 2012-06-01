using Castle.ActiveRecord.Attributes;
using NHibernate.Event;

namespace Castle.ActiveRecord.Tests.Event {
	[EventListener]
	public class SamplePostDeleteListener : IPostDeleteEventListener
	{
		public void OnPostDelete(PostDeleteEvent @event) { }
	}
}