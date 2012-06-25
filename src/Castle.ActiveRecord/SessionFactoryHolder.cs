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
using Castle.ActiveRecord.Scopes;
using Castle.Core.Internal;
using Iesi.Collections;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Metadata;

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

		readonly IDictionary<Type, SfHolder> type2SessFactory = new ConcurrentDictionary<Type, SfHolder>();
		IThreadScopeInfo threadScopeInfo;

		/// <summary>
		/// Requests the Configuration associated to the type.
		/// </summary>
		public Configuration GetConfiguration(Type type)
		{
			return type2SessFactory.ContainsKey(type) ? type2SessFactory[type].Configuration : GetConfiguration(type.BaseType);
		}

		/// <summary>
		/// Pendent
		/// </summary>
		public Configuration[] GetAllConfigurations()
		{
			return type2SessFactory.Values.Select(s => s.Configuration).Distinct().ToArray();
		}

		/// <summary>
		/// Requests the registered types
		/// </summary>
		public Type[] GetRegisteredTypes()
		{
			return type2SessFactory.Keys.ToArray();
		}

		/// <summary>
		/// Gets the all the session factories.
		/// </summary>
		/// <returns></returns>
		public ISessionFactory[] GetSessionFactories()
		{
			return type2SessFactory.Values.Select(sf => sf.SessionFactory).Distinct().ToArray();
		}

		/// <summary>
		/// Optimized with reader/writer lock.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public ISessionFactory GetSessionFactory(Type type)
		{
			if (type == null || !type2SessFactory.ContainsKey(type))
			{
				throw new ActiveRecordException("No configuration for ActiveRecord found in the type hierarchy -> " + type.FullName);
			}

			var sessFactory = type2SessFactory[type].SessionFactory;

			if (sessFactory != null)
			{
				return sessFactory;
			}

			var cfg = GetConfiguration(type);

			sessFactory = cfg.BuildSessionFactory();

			type2SessFactory[type] = new SfHolder(cfg, sessFactory);

			return sessFactory;
		}

		/// <summary>
		/// Obtains the IClassMetadata of the type.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public IClassMetadata GetClassMetadata(Type type) {
			if (type == null || !type2SessFactory.ContainsKey(type))
			{
				return null;
			}
			return type2SessFactory[type].SessionFactory.GetClassMetadata(type);
		}

		///<summary>
		/// This method allows direct registration of Configuration
		///</summary>
		public void RegisterConfiguration(Configuration cfg)
		{
			var sf = cfg.BuildSessionFactory();
			var sfholder = new SfHolder(cfg, sf);

			foreach (var classMetadata in sf.GetAllClassMetadata()) {
				var entitytype = classMetadata.Value.GetMappedClass(EntityMode.Poco);

				if (!type2SessFactory.ContainsKey(entitytype))
					type2SessFactory.Add(entitytype, sfholder);
			}
		}

		/// <summary>
		/// Creates a session for the associated type
		/// </summary>
		[MethodImpl(MethodImplOptions.Synchronized)]
		public ISession CreateSession(Type type)
		{
			if (threadScopeInfo.HasInitializedScope)
			{
				return CreateScopeSession(type);
			}

			// Create a sessionscope implicitly
			new SessionScope();

			return CreateSession(type);

			/*
			ISessionFactory sessionFactory = GetSessionFactory(type);

			ISession session = OpenSession(sessionFactory);

			return session;
			 */
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
			if (threadScopeInfo.HasInitializedScope) return;

			session.Flush();
			session.Dispose();
		}

		/// <summary>
		/// Called if an action on the session fails
		/// </summary>
		/// <param name="session"></param>
		public void FailSession(ISession session)
		{
			if (threadScopeInfo.HasInitializedScope)
			{
				ISessionScope scope = threadScopeInfo.GetRegisteredScope();
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
		public IThreadScopeInfo ThreadScopeInfo
		{
			get { return threadScopeInfo; }
			set
			{
				ThreadScopeAccessor.Instance.ScopeInfo = value;
				threadScopeInfo = value;
			}
		}

		private ISession CreateScopeSession(Type type)
		{
			ISessionScope scope = threadScopeInfo.GetRegisteredScope();
			ISessionFactory sessionFactory = GetSessionFactory(type);
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
			type2SessFactory.Values.ForEach(sf => sf.SessionFactory.Dispose());
			type2SessFactory.Clear();
		}
	}
}
