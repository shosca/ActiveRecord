using Castle.ActiveRecord.Attributes;
using Castle.ActiveRecord.Tests.Models;
using NHibernate.Event;
using NHibernate.Event.Default;

namespace Castle.ActiveRecord.Tests.Event {
	[EventListener]
	public class FirstBaseListener : IPreLoadEventListener
	{
		public void OnPreLoad(PreLoadEvent @event) { }
	}
}