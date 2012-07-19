// Copyright 2004-2011 Castle Project - http://www.castleproject.org/
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reflection;
using Castle.ActiveRecord.Attributes;
using Castle.Core.Internal;
using NHibernate.Cfg;
using NHibernate.Mapping.ByCode;

namespace Castle.ActiveRecord.Config {
	public class SessionFactoryConfig {
		public SessionFactoryConfig(IActiveRecordConfiguration source) {
			Assemblies = new List<Assembly>();
			Contributors = new List<INHContributor>();
			Properties = new NameValueCollection();
			Name = string.Empty;
			Source = source;
		}

		public IActiveRecordConfiguration Source { get; private set; }
		public string Name { get; set; }
		public IList<Assembly> Assemblies { get; private set; }
		public NameValueCollection Properties { get; private set; }
		public IList<INHContributor> Contributors { get; private set; }

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

		public IEnumerable<INHContributor> GetContributors() {
			var contributors = Assemblies.SelectMany(a => a.GetExportedTypes())
				.Where(t => typeof (INHContributor).IsAssignableFrom(t))
				.Select(c => (INHContributor) Activator.CreateInstance(c))
				.ToList();

			return contributors;
		}

		public Configuration BuildConfiguration() {
			var mapper = new ConventionModelMapper();

			CollectMappingContributorsAndApply(mapper);

			AR.RaiseOnMapperCreated(mapper, this);

			var mapping = mapper.CompileMappingForAllExplicitlyAddedEntities();
			mapping.autoimport = Source.AutoImport;
			mapping.defaultlazy = Source.Lazy;

			AR.RaiseOnHbmMappingCreated(mapping, this);

			var cfg = new Configuration();

			foreach(var key in Properties.AllKeys)
			{
				cfg.Properties[key] = Properties[key];
			}

			CollectAllContributorsAndRegister(cfg);

			cfg.AddMapping(mapping);

			AR.RaiseOnConfigurationCreated(cfg, this);

			return cfg;
		}

		void CollectMappingContributorsAndApply(ModelMapper mapper) {
			Assemblies.SelectMany(a => a.GetExportedTypes())
				.Where(t => !t.IsInterface && !t.IsAbstract && typeof (IMappingContributor).IsAssignableFrom(t))
				.Select(t => (IMappingContributor) Activator.CreateInstance(t))
				.ForEach(m => m.Contribute(mapper));
		}

		void CollectAllContributorsAndRegister(Configuration cfg) {
			var exportedtypes = Assemblies.SelectMany(a => a.GetTypes()).ToArray();

			Contributors.Add(GetEventListenerContributor(exportedtypes));
			foreach(var c in Assemblies.SelectMany(a => a.GetExportedTypes())
								.Where(t => !t.IsInterface && !t.IsAbstract && typeof (INHContributor).IsAssignableFrom(t))
								.Select(c => (INHContributor) Activator.CreateInstance(c))) {
				Contributors.Add(c);
			}

			foreach (var nhContributor in Contributors) {
				nhContributor.Contribute(cfg);
			}
		}

		static INHContributor GetEventListenerContributor(IEnumerable<Type> exportedtypes)
		{
			var contributor = new EventListenerContributor();
			foreach (var type in exportedtypes)
			{
				var eventListenerAttributes = type.GetCustomAttributes(typeof(EventListenerAttribute), false);
				if (eventListenerAttributes.Length == 1)
				{
					var attribute = (EventListenerAttribute)eventListenerAttributes[0];
					var config = new EventListenerConfig(type)
					{
						ReplaceExisting = attribute.ReplaceExisting,
						SkipEvent = attribute.SkipEvent,
						Singleton = attribute.Singleton
					};

					contributor.Add(config);
				}
			}

			return contributor;
		}

		public void AddContributor(INHContributor contributor) {
			Contributors.Add(contributor);
		}
	}
}
