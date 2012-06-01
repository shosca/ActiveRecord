using Castle.ActiveRecord.Attributes;
using NHibernate.Event;

namespace Castle.ActiveRecord.Tests.Event {
	[EventListener(ReplaceExisting = false)]
	public class AdditionalLoadListener : ILoadEventListener
	{
		public void OnLoad(LoadEvent @event, LoadType loadType){}
	}
}