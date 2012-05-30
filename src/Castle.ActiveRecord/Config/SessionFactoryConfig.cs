using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reflection;

namespace Castle.ActiveRecord.Config {
	public class SessionFactoryConfig {
		public SessionFactoryConfig() {
			Assemblies = new List<Assembly>();
			Properties = new NameValueCollection();
			Name = string.Empty;
		}

		public string Name { get; set; }
		public IList<Assembly> Assemblies { get; set; }
		public NameValueCollection Properties { get; set; }
	}
}