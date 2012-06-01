using Castle.ActiveRecord.Attributes;
using NHibernate.Event;

namespace Castle.ActiveRecord.Tests.Event {
	[EventListener]
	public class SamplePreInsertListener : IPreInsertEventListener
	{
		public bool OnPreInsert(PreInsertEvent @event) { return false; }
	}
}