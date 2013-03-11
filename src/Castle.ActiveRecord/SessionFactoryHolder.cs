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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Collections;
using System.Runtime.CompilerServices;
using Castle.ActiveRecord.Config;
using Castle.ActiveRecord.Scopes;
using Castle.Core.Internal;
using Iesi.Collections;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Metadata;
using NHibernate.Proxy;

namespace Castle.ActiveRecord
{
	/// <summary>
	/// Default implementation of <seealso cref="ISessionFactoryHolder"/>
	/// </summary>
	/// <remarks>
	/// This class is thread safe
	/// </remarks>
	public class SessionFactoryHolder : MarshalByRefObject, ISessionFactoryHolder {
	    public class SfHolder {
			public SfHolder(Configuration config, ISessionFactory sf) {
				if(config == null || sf == null)
					throw new InvalidOperationException();

				Configuration = config;
				SessionFactory = sf;
			}
			public Configuration Configuration { get; private set; }
			public ISessionFactory SessionFactory { get; private set; }
		}


		/// <summary>
		/// Gets or sets the implementation of <see cref="IThreadScopeInfo"/>
		/// </summary>
		/// <value></value>
		public virtual IThreadScopeInfo ThreadScopeInfo { get; private set; }
        public IActiveRecordConfiguration ConfigurationSource { get; protected set; }

        protected readonly ISet<Assembly> RegisteredAssemblies = new HashSet<Assembly>();
		protected readonly ConcurrentDictionary <Type, Model> Type2Model = new ConcurrentDictionary<Type, Model>();
		protected readonly IDictionary<Type, SfHolder> Type2SessFactory = new ConcurrentDictionary<Type, SfHolder>();

	    public SessionFactoryHolder(IActiveRecordConfiguration source) {
	        ConfigurationSource = source;
	        ThreadScopeInfo = CreateThreadScopeInfoImplementation(source);

            foreach (var key in source.GetAllConfigurationKeys()) {

                var config = source.GetConfiguration(key);

                foreach (var asm in config.Assemblies) {
                    if (RegisteredAssemblies.Contains(asm))
                        throw new ActiveRecordException(string.Format("Assembly {0} has already been registered.", asm));

                }

                RegisterConfiguration(config);
            }
	    }

	    /// <summary>
		/// Requests the Configuration associated to the type.
		/// </summary>
		public virtual Configuration GetConfiguration(Type type)
		{
			return Type2SessFactory.ContainsKey(type) ? Type2SessFactory[type].Configuration : GetConfiguration(type.BaseType);
		}

		/// <summary>
		/// Pendent
		/// </summary>
		public virtual Configuration[] GetAllConfigurations()
		{
			return Type2SessFactory.Values.Select(s => s.Configuration).Distinct().ToArray();
		}

		/// <summary>
		/// Requests the registered types
		/// </summary>
		public virtual Type[] GetRegisteredTypes()
		{
			return Type2SessFactory.Keys.ToArray();
		}

		public virtual bool IsInitialized(Type type)
		{
			type = GetNonProxyType(type);
			return Type2SessFactory.ContainsKey(type);
		}

		public virtual Model GetModel(Type type)
		{
			type = GetNonProxyType(type);

			return Type2SessFactory.ContainsKey(type)
				? Type2Model.GetOrAdd(type, t => {
					var sf = GetSessionFactory(t);
					var model = new Model(sf, type);
					return model;
				})
				: null;
		}

		public virtual IClassMetadata GetClassMetadata(Type type) {
			type = GetNonProxyType(type);
			return Type2SessFactory.ContainsKey(type) 
				? Type2SessFactory[type].SessionFactory.GetClassMetadata(type)
				: null;
		}

		/// <summary>
		/// Gets the all the session factories.
		/// </summary>
		/// <returns></returns>
		public virtual ISessionFactory[] GetSessionFactories()
		{
			return Type2SessFactory.Values.Select(sf => sf.SessionFactory).Distinct().ToArray();
		}

		/// <summary>
		/// Returns ISessionFactory of a registered type
		/// </summary>
		public virtual ISessionFactory GetSessionFactory(Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			type = GetNonProxyType(type);
			if (!Type2SessFactory.ContainsKey(type))
			{
				throw new ActiveRecordException("No configuration for ActiveRecord found in the type hierarchy -> " + type.FullName);
			}

			var sessFactory = Type2SessFactory[type].SessionFactory;

			if (sessFactory != null)
			{
				return sessFactory;
			}

			var cfg = GetConfiguration(type);

			sessFactory = cfg.BuildSessionFactory();

			Type2SessFactory[type] = new SfHolder(cfg, sessFactory);

			return sessFactory;
		}

		///<summary>
		/// This method allows direct registration of Configuration
		///</summary>
		public virtual void RegisterConfiguration(Configuration cfg, string name)
		{
			var sf = cfg.BuildSessionFactory();
			var sfholder = new SfHolder(cfg, sf);

			foreach (var classMetadata in sf.GetAllClassMetadata()) {
				var entitytype = classMetadata.Value.GetMappedClass(EntityMode.Poco);

				if (Type2SessFactory.ContainsKey(entitytype))
					throw new ActiveRecordException("Type has already been registered -> " + entitytype.FullName);
				
				Type2SessFactory.Add(entitytype, sfholder);
			}
			AR.RaiseSessionFactoryCreated(sf, cfg, name);
		}

		protected void RegisterConfiguration(SessionFactoryConfig config) {
			var cfg = config.BuildConfiguration();
			RegisterConfiguration(cfg, config.Name);
		}

		public virtual void Dispose() {
			Type2SessFactory.Values.ForEach(sf => sf.SessionFactory.Dispose());
			Type2SessFactory.Clear();
		}

		public virtual Type GetNonProxyType(Type type) {
			return typeof(INHibernateProxy).IsAssignableFrom(type)
				? GetNonProxyType(type.BaseType)
				: type;
		}

        IThreadScopeInfo CreateThreadScopeInfoImplementation(IActiveRecordConfiguration source)
        {
            if (source.ThreadScopeInfoImplementation == null)
                return new ThreadScopeInfo();

            var threadScopeType = source.ThreadScopeInfoImplementation;

            if (!typeof(IThreadScopeInfo).IsAssignableFrom(threadScopeType))
            {
                var message = String.Format("The specified type {0} does " + "not implement the interface IThreadScopeInfo", threadScopeType.FullName);

                throw new ActiveRecordInitializationException(message);
            }

            return (IThreadScopeInfo) Activator.CreateInstance(threadScopeType);
        }
	}
}
