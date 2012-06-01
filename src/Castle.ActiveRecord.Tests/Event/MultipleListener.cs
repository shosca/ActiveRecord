using Castle.ActiveRecord.Attributes;
using NHibernate.Event;
using NHibernate.Event.Default;

namespace Castle.ActiveRecord.Tests.Event {
	[EventListener]
	public class MultipleListener : IPreLoadEventListener, IPostLoadEventListener
	{
		public void OnPreLoad(PreLoadEvent @event) { }
		public void OnPostLoad(PostLoadEvent @event) { }
	}
}