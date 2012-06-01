using Castle.ActiveRecord.Attributes;
using NHibernate.Event;

namespace Castle.ActiveRecord.Tests.Event {
	[EventListener]
	public class SamplePostInsertListener : IPostInsertEventListener
	{
		public void OnPostInsert(PostInsertEvent @event) { }
	}
}