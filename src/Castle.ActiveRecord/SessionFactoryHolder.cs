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
		internal class SfHolder {
			public SfHolder(Configuration config, ISessionFactory sf) {
				if(config == null || sf == null)
					throw new InvalidOperationException();

				Configuration = config;
				SessionFactory = sf;
			}
			public Configuration Configuration { get; private set; }
			public ISessionFactory SessionFactory { get; private set; }
		}

		readonly ConcurrentDictionary<Type, Model> _type2Model = new ConcurrentDictionary<Type, Model>();
		readonly IDictionary<Type, SfHolder> _type2SessFactory = new ConcurrentDictionary<Type, SfHolder>();

		/// <summary>
		/// Requests the Configuration associated to the type.
		/// </summary>
		public Configuration GetConfiguration(Type type)
		{
			return _type2SessFactory.ContainsKey(type) ? _type2SessFactory[type].Configuration : GetConfiguration(type.BaseType);
		}

		/// <summary>
		/// Pendent
		/// </summary>
		public Configuration[] GetAllConfigurations()
		{
			return _type2SessFactory.Values.Select(s => s.Configuration).Distinct().ToArray();
		}

		/// <summary>
		/// Requests the registered types
		/// </summary>
		public Type[] GetRegisteredTypes()
		{
			return _type2SessFactory.Keys.ToArray();
		}

		public bool IsInitialized(Type type)
		{
			type = GetNonProxyType(type);
			return _type2SessFactory.ContainsKey(type);
		}

		public Model GetModel(Type type)
		{
			type = GetNonProxyType(type);

			return _type2SessFactory.ContainsKey(type)
				? _type2Model.GetOrAdd(type, t => {
					var sf = GetSessionFactory(t);
					var model = new Model(sf, type);
					return model;
				})
				: null;
		}

		public IClassMetadata GetClassMetadata(Type type) {
			type = GetNonProxyType(type);
			return _type2SessFactory.ContainsKey(type) 
				? _type2SessFactory[type].SessionFactory.GetClassMetadata(type)
				: null;
		}

		/// <summary>
		/// Gets the all the session factories.
		/// </summary>
		/// <returns></returns>
		public ISessionFactory[] GetSessionFactories()
		{
			return _type2SessFactory.Values.Select(sf => sf.SessionFactory).Distinct().ToArray();
		}

		/// <summary>
		/// Optimized with reader/writer lock.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public ISessionFactory GetSessionFactory(Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			type = GetNonProxyType(type);
			if (!_type2SessFactory.ContainsKey(type))
			{
				throw new ActiveRecordException("No configuration for ActiveRecord found in the type hierarchy -> " + type.FullName);
			}

			var sessFactory = _type2SessFactory[type].SessionFactory;

			if (sessFactory != null)
			{
				return sessFactory;
			}

			var cfg = GetConfiguration(type);

			sessFactory = cfg.BuildSessionFactory();

			_type2SessFactory[type] = new SfHolder(cfg, sessFactory);

			return sessFactory;
		}

		///<summary>
		/// This method allows direct registration of Configuration
		///</summary>
		public void RegisterConfiguration(Configuration cfg, string name)
		{
			var sf = cfg.BuildSessionFactory();
			var sfholder = new SfHolder(cfg, sf);

			foreach (var classMetadata in sf.GetAllClassMetadata()) {
				var entitytype = classMetadata.Value.GetMappedClass(EntityMode.Poco);

				if (!_type2SessFactory.ContainsKey(entitytype))
					_type2SessFactory.Add(entitytype, sfholder);
			}
			AR.RaiseSessionFactoryCreated(sf, name);
		}

		public void RegisterConfiguration(SessionFactoryConfig config) {
			var cfg = config.BuildConfiguration();
			RegisterConfiguration(cfg, config.Name);
		}

		/// <summary>
		/// Creates a session for the associated type
		/// </summary>
		[MethodImpl(MethodImplOptions.Synchronized)]
		public ISession CreateSession(Type type)
		{
			if (ThreadScopeInfo.HasInitializedScope)
			{
				return CreateScopeSession(type);
			}

			// Create a sessionscope implicitly
			new SessionScope();

			return CreateSession(type);
		}

		private static ISession OpenSession(ISessionFactory sessionFactory)
		{
			lock(sessionFactory)
			{
				return sessionFactory.OpenSession(InterceptorFactory.Create());
			}
		}

		internal static ISession OpenSessionWithScope(ISessionScope scope, ISessionFactory sessionFactory)
		{
			lock(sessionFactory)
			{
				return scope.OpenSession(sessionFactory, InterceptorFactory.Create());
			}
		}

		/// <summary>
		/// Releases the specified session
		/// </summary>
		/// <param name="session"></param>
		public void ReleaseSession(ISession session)
		{
			if (ThreadScopeInfo.HasInitializedScope) return;

			session.Flush();
			session.Dispose();
		}

		/// <summary>
		/// Called if an action on the session fails
		/// </summary>
		/// <param name="session"></param>
		public void FailSession(ISession session)
		{
			if (ThreadScopeInfo.HasInitializedScope)
			{
				var scope = ThreadScopeInfo.GetRegisteredScope();
				scope.FailSession(session);
			}
			else
			{
				session.Clear();
			}
		}

		/// <summary>
		/// Gets or sets the implementation of <see cref="IThreadScopeInfo"/>
		/// </summary>
		/// <value></value>
		public IThreadScopeInfo ThreadScopeInfo { get; set; }

		private ISession CreateScopeSession(Type type)
		{
			var scope = ThreadScopeInfo.GetRegisteredScope();
			var sessionFactory = GetSessionFactory(type);
#if DEBUG
			System.Diagnostics.Debug.Assert(scope != null);
			System.Diagnostics.Debug.Assert(sessionFactory != null);
#endif
			if (scope.IsKeyKnown(sessionFactory))
			{
				return scope.GetSession(sessionFactory);
			}

			ISession session;

			session = scope.WantsToCreateTheSession
				? OpenSessionWithScope(scope, sessionFactory)
				: OpenSession(sessionFactory);
#if DEBUG
			System.Diagnostics.Debug.Assert(session != null);
#endif
			scope.RegisterSession(sessionFactory, session);

			return session;
		}

		public void Dispose() {
			_type2SessFactory.Values.ForEach(sf => sf.SessionFactory.Dispose());
			_type2SessFactory.Clear();
		}

		public Type GetNonProxyType(Type type) {
			return typeof(INHibernateProxy).IsAssignableFrom(type)
				? GetNonProxyType(type.BaseType)
				: type;
		}
	}
}
