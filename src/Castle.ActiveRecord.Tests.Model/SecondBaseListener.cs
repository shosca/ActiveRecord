using Castle.ActiveRecord.Attributes;
using NHibernate.Event;
using NHibernate.Event.Default;

namespace Castle.ActiveRecord.Tests.Model {
	[EventListener]
	public class SecondBaseListener : IPreLoadEventListener
	{
		public void OnPreLoad(PreLoadEvent @event) { }
	}

	[EventListener]
	public class SecondAdditionalLoadListener : ILoadEventListener
	{
		public void OnLoad(LoadEvent @event, LoadType loadType) { }
	}
}