using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reflection;
using Castle.ActiveRecord.ByteCode;
using NHibernate.Cfg;

namespace Castle.ActiveRecord.Config {
	public class SessionFactoryConfig {
		public SessionFactoryConfig(IActiveRecordConfiguration source) {
			Assemblies = new List<Assembly>();
			Properties = new NameValueCollection();
			Name = string.Empty;
			this.Source = source;
			Properties[Environment.ProxyFactoryFactoryClass] = typeof (ARProxyFactoryFactory).AssemblyQualifiedName;
		}

		public IActiveRecordConfiguration Source { get; private set; }
		public string Name { get; set; }
		public IList<Assembly> Assemblies { get; private set; }
		public NameValueCollection Properties { get; private set; }

		public SessionFactoryConfig AddAssembly(Assembly assembly) {
			Assemblies.Add(assembly);
			return this;
		}

		public SessionFactoryConfig AddAssemblies(IEnumerable<Assembly> assemblies) {
			foreach (var asm in assemblies) {
				Assemblies.Add(asm);
			}
			return this;
		}

		public SessionFactoryConfig Set(string key, string value) {
			Properties[key] = value;
			return this;
		}

		public IActiveRecordConfiguration End() {
			return Source;
		}

		public SessionFactoryConfig Set(IDictionary<string, string> properties) {
			foreach (var property in properties) {
				Set(property.Key, property.Value);
			}
			return this;
		}
	}
}
