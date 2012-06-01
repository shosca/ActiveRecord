using Castle.ActiveRecord.Attributes;
using NHibernate.Event;
using NHibernate.Event.Default;

namespace Castle.ActiveRecord.Tests.Model {
	[EventListener(ReplaceExisting = true)]
	public class ReplacementLoadListener : DefaultLoadEventListener, ILoadEventListener
	{
	}
}