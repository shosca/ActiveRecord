using Castle.ActiveRecord.Attributes;
using NHibernate.Event;
using NHibernate.Event.Default;

namespace Castle.ActiveRecord.Tests.Event {
	[EventListener(Singleton = true)]
	public class MultipleSingletonListener : IPreLoadEventListener, IPostLoadEventListener
	{
		public void OnPostLoad(PostLoadEvent @event){ }
		public void OnPreLoad(PreLoadEvent @event) { }
	}
}