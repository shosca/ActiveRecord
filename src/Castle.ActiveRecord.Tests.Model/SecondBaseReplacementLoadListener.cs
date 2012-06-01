using Castle.ActiveRecord.Attributes;
using NHibernate.Event;
using NHibernate.Event.Default;

namespace Castle.ActiveRecord.Tests.Model {
	[EventListener(ReplaceExisting = true)]
	public class SecondBaseReplacementLoadListener : ILoadEventListener {
		public void OnLoad(LoadEvent @event, LoadType loadType) { }
	}
}