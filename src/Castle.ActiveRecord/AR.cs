// Copyright 2003-2011 Castle Project - http://www.castleproject.org/
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


using System.Linq;
using NHibernate;
using NHibernate.Cfg.MappingSchema;

namespace Castle.ActiveRecord {
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Reflection;
	using Castle.ActiveRecord.Config;
	using Castle.ActiveRecord.Scopes;
	using NHibernate.Cfg;
	using NHibernate.Mapping.ByCode;
	using NHibernate.Tool.hbm2ddl;
	using Environment = NHibernate.Cfg.Environment;

	/// <summary>
	/// Performs the framework initialization.
	/// </summary>
	/// <remarks>
	/// This class is not thread safe.
	/// </remarks>
	public static partial class AR
	{
		#region Configuration/Registeration

		private static readonly ISet<Assembly> RegisteredAssemblies = new HashSet<Assembly>();
		private static readonly Object LockConfig = new object();

		public static IActiveRecordConfiguration ConfigurationSource { get; private set; }

		/// <summary>
		/// The global holder for the session factories.
		/// </summary>
		public static ISessionFactoryHolder Holder { get; private set; }

		/// <summary>
		/// So others frameworks can intercept the 
		/// creation and act on the holder instance
		/// </summary>
		public static event SessionFactoryHolderDelegate OnSessionFactoryHolderCreated;

		public static event SessionFactoryDelegate OnSessionFactoryCreated;

		/// <summary>
		/// Allows other frameworks to modify the ModelMapper
		/// before the generation of the NHibernate configuration.
		/// As an example, this may be used to rewrite table names to
		/// conform to an application-specific standard.  Since the
		/// configuration source is passed in, it is possible to
		/// determine the underlying database type and make changes
		/// if necessary.
		/// </summary>
		public static event MapperDelegate OnMapperCreated;

		/// <summary>
		/// 
		/// </summary>
		public static event HbmMappingDelegate OnHbmMappingCreated;

		/// <summary>
		/// 
		/// </summary>
		public static event ConfigurationDelegate OnConfigurationCreated;


		/// <summary>
		/// Initialize the mappings using the configuration and 
		/// checking all the types on the specified Assemblies
		/// </summary>
		public static void Initialize(IActiveRecordConfiguration source)
		{
			CreateSessionFactoryAndRegisterToHolder(source);
		}

		/// <summary>
		/// Initializes the framework reading the configuration from
		/// the <c>AppDomain</c> and checking all the types on the executing <c>Assembly</c>
		/// </summary>
		public static void Initialize()
		{
			var source = ActiveRecordSectionHandler.Instance;

			Initialize(source);
		}

		/// <summary>
		/// Initialize the mappings using the configuration and 
		/// the list of types
		/// </summary>
		private static void CreateSessionFactoryAndRegisterToHolder(IActiveRecordConfiguration source)
		{
			if (source == null)
			{
				throw new ArgumentNullException("source");
			}

			lock(LockConfig)
			{
				if (Holder == null) {
					// First initialization
					Holder = CreateSessionFactoryHolderImplementation(source);
					Holder.ThreadScopeInfo = CreateThreadScopeInfoImplementation(source);
					RaiseSessionFactoryHolderCreated(Holder);
				}

				ConfigurationSource = source;

				foreach (var key in source.GetAllConfigurationKeys()) {

					var config = source.GetConfiguration(key);

					foreach (var asm in config.Assemblies) {
						if (RegisteredAssemblies.Contains(asm))
							throw new ActiveRecordException(string.Format("Assembly {0} has already been registered.", asm));

					}

					Holder.RegisterConfiguration(config);
				}

			}
		}

		/// <summary>
		/// Builds a fluent configuration for general ActiveRecord settings.
		/// </summary>
		public static DefaultActiveRecordConfiguration Configure()
		{
			return new DefaultActiveRecordConfiguration();
		}

		/// <summary>
		/// Generates and executes the creation scripts for the database.
		/// </summary>
		public static void CreateSchema()
		{
			CheckInitialized();

			foreach(var config in Holder.GetAllConfigurations())
			{
				var export = CreateSchemaExport(config);

				try
				{
					export.Create(false, true);
				}
				catch(Exception ex)
				{
					throw new ActiveRecordException("Could not create the schema", ex);
				}
			}
		}

		/// <summary>
		/// Generates and executes the creation scripts for the database using 
		/// the specified baseClass to know which database it should create the schema for.
		/// </summary>
		public static void CreateSchema(Type baseClass)
		{
			CheckInitialized();

			var config = Holder.GetConfiguration(baseClass);

			var export = CreateSchemaExport(config);

			try
			{
				export.Create(false, true);
			}
			catch(Exception ex)
			{
				throw new ActiveRecordException("Could not create the schema", ex);
			}
		}

		/// <summary>
		/// Generates and executes the Drop scripts for the database.
		/// </summary>
		public static void DropSchema()
		{
			CheckInitialized();

			foreach(var config in Holder.GetAllConfigurations())
			{
				var export = CreateSchemaExport(config);

				try
				{
					export.Drop(false, true);
				}
				catch(Exception ex)
				{
					throw new ActiveRecordException("Could not drop the schema", ex);
				}
			}
		}

		/// <summary>
		/// Generates and executes the Drop scripts for the database using 
		/// the specified baseClass to know which database it should create the scripts for.
		/// </summary>
		public static void DropSchema(Type baseClass)
		{
			CheckInitialized();

			Configuration config = Holder.GetConfiguration(baseClass);

			SchemaExport export = CreateSchemaExport(config);

			try
			{
				export.Drop(false, true);
			}
			catch(Exception ex)
			{
				throw new ActiveRecordException("Could not drop the schema", ex);
			}
		}

		/// <summary>
		/// Generates and executes the creation scripts for the database.
		/// </summary>
		/// <returns>List of exceptions that occurred during the update process</returns>
		public static IList<Exception> UpdateSchema()
		{
			CheckInitialized();
			var exceptions = new List<Exception>();

			foreach(var config in Holder.GetAllConfigurations())
			{
				var updater = CreateSchemaUpdate(config);

				try
				{
					updater.Execute(false, true);

					exceptions.AddRange(updater.Exceptions);
				}
				catch(Exception ex)
				{
					throw new ActiveRecordException("Could not update the schema", ex);
				}
			}

			return exceptions;
		}

		/// <summary>
		/// Generates and executes the creation scripts for the database using 
		/// the specified baseClass to know which database it should create the schema for.
		/// </summary>
		public static IList<Exception> UpdateSchema(Type baseClass)
		{
			CheckInitialized();

			var config = Holder.GetConfiguration(baseClass);

			var updater = CreateSchemaUpdate(config);

			try
			{
				updater.Execute(false, true);
			}
			catch(Exception ex)
			{
				throw new ActiveRecordException("Could not update the schema", ex);
			}

			return updater.Exceptions;
		}

		/// <summary>
		/// Generates the drop scripts for the database saving them to the supplied file name. 
		/// </summary>
		/// <remarks>
		/// If ActiveRecord was configured to access more than one database, a file is going
		/// to be generate for each, based on the path and the <c>fileName</c> specified.
		/// </remarks>
		public static void GenerateDropScripts(String fileName)
		{
			CheckInitialized();

			var isFirstExport = true;
			var fileCount = 1;

			foreach(var config in Holder.GetAllConfigurations())
			{
				var export = CreateSchemaExport(config);

				try
				{
					export.SetOutputFile(isFirstExport ? fileName : CreateAnotherFile(fileName, fileCount++));
					export.Drop(false, false);
				}
				catch(Exception ex)
				{
					throw new ActiveRecordException("Could not drop the schema", ex);
				}

				isFirstExport = false;
			}
		}

		/// <summary>
		/// Generates the drop scripts for the database saving them to the supplied file name. 
		/// The baseType is used to identify which database should we act upon.
		/// </summary>
		public static void GenerateDropScripts(Type baseType, String fileName)
		{
			CheckInitialized();

			var config = Holder.GetConfiguration(baseType);

			var export = CreateSchemaExport(config);

			try
			{
				export.SetOutputFile(fileName);
				export.Drop(false, false);
			}
			catch(Exception ex)
			{
				throw new ActiveRecordException("Could not generate drop schema scripts", ex);
			}
		}

		/// <summary>
		/// Generates the creation scripts for the database
		/// </summary>
		/// <remarks>
		/// If ActiveRecord was configured to access more than one database, a file is going
		/// to be generate for each, based on the path and the <c>fileName</c> specified.
		/// </remarks>
		public static void GenerateCreationScripts(String fileName)
		{
			CheckInitialized();

			var isFirstExport = true;
			var fileCount = 1;

			foreach(var export in Holder.GetAllConfigurations().Select(c => CreateSchemaExport(c)))
			{
				try
				{
					export.SetOutputFile(isFirstExport ? fileName : CreateAnotherFile(fileName, fileCount++));
					export.Create(false, false);
				}
				catch(Exception ex)
				{
					throw new ActiveRecordException("Could not create the schema", ex);
				}

				isFirstExport = false;
			}
		}

		/// <summary>
		/// Generates the creation scripts for the database
		/// The baseType is used to identify which database should we act upon.
		/// </summary>
		public static void GenerateCreationScripts(Type baseType, String fileName)
		{
			CheckInitialized();

			var config = Holder.GetConfiguration(baseType);

			var export = CreateSchemaExport(config);

			try
			{
				export.SetOutputFile(fileName);
				export.Create(false, false);
			}
			catch(Exception ex)
			{
				throw new ActiveRecordException("Could not create the schema scripts", ex);
			}
		}

		/// <summary>
		/// Intended to be used only by test cases
		/// </summary>
		public static void ResetInitialization()
		{
			// Make sure we start with it enabled
			Environment.UseReflectionOptimizer = true;
			if (Holder != null) Holder.Dispose();
			Holder = null;
		}

		/// <summary>
		/// Gets a value indicating whether ActiveRecord was initialized properly (see the Initialize method).
		/// </summary>
		/// <value>
		/// 	<c>true</c> if it is initialized; otherwise, <c>false</c>.
		/// </value>
		public static bool IsInitialized
		{
			get { return Holder != null; }
		}

		private static SchemaExport CreateSchemaExport(Configuration cfg)
		{
			var export = new SchemaExport(cfg);
			return export;
		}

		private static SchemaUpdate CreateSchemaUpdate(Configuration cfg)
		{
			return new SchemaUpdate(cfg);
		}

		private static void CheckInitialized()
		{
			if (Holder == null)
			{
				throw new ActiveRecordException("Framework must be Initialized first.");
			}
		}


		private static ISessionFactoryHolder CreateSessionFactoryHolderImplementation(IActiveRecordConfiguration source)
		{
			if (source.SessionFactoryHolderImplementation == null)
				return new SessionFactoryHolder();

			var sessionFactoryHolderType = source.SessionFactoryHolderImplementation;

			if (!typeof(ISessionFactoryHolder).IsAssignableFrom(sessionFactoryHolderType))
			{
				var message =
					String.Format("The specified type {0} does " + "not implement the interface ISessionFactoryHolder",
					              sessionFactoryHolderType.FullName);

				throw new ActiveRecordException(message);
			}

			return (ISessionFactoryHolder) Activator.CreateInstance(sessionFactoryHolderType);
		}

		private static IThreadScopeInfo CreateThreadScopeInfoImplementation(IActiveRecordConfiguration source)
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


		/// <summary>
		/// Generate a file name based on the original file name specified, using the 
		/// count to give it some order.
		/// </summary>
		/// <param name="originalFileName"></param>
		/// <param name="fileCount"></param>
		/// <returns></returns>
		private static string CreateAnotherFile(string originalFileName, int fileCount)
		{
			var path = Path.GetDirectoryName(originalFileName);
			var fileName = Path.GetFileNameWithoutExtension(originalFileName);
			var extension = Path.GetExtension(originalFileName);

			return Path.Combine(path, string.Format("{0}_{1}{2}", fileName, fileCount, extension));
		}

		private static void RaiseSessionFactoryHolderCreated(ISessionFactoryHolder holder) {
			if (OnSessionFactoryHolderCreated != null) {
				OnSessionFactoryHolderCreated(holder);
			}
		}

		internal static void RaiseOnMapperCreated(ConventionModelMapper mapper, SessionFactoryConfig sessionFactoryConfig) {
			if (OnMapperCreated != null)
				OnMapperCreated(mapper, sessionFactoryConfig);
		}

		internal static void RaiseOnConfigurationCreated(Configuration cfg, SessionFactoryConfig sessionFactoryConfig) {
			if (OnConfigurationCreated != null)
				OnConfigurationCreated(cfg, sessionFactoryConfig);
		}

		internal static void RaiseOnHbmMappingCreated(HbmMapping mapping, SessionFactoryConfig sessionFactoryConfig) {
			if (OnHbmMappingCreated != null)
				OnHbmMappingCreated(mapping, sessionFactoryConfig);
		}

		internal static void RaiseSessionFactoryCreated(ISessionFactory sf, string name) {
			if (OnSessionFactoryCreated != null)
				OnSessionFactoryCreated(sf, name);
		}

		#endregion

		#region Execute/ExecuteStateless

		/// <summary>
		/// Invokes the specified delegate passing a valid 
		/// NHibernate session. Used for custom NHibernate queries.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="func">The delegate instance</param>
		/// <param name="instance">The ActiveRecord instance</param>
		/// <returns>Whatever is returned by the delegate invocation</returns>
		public static TK Execute<T, TK>(Type type, Func<ISession, T, TK> func, T instance) {
			if (func == null) throw new ArgumentNullException("func", "Delegate must be passed");

			EnsureInitialized(type);

			var session = Holder.CreateSession(type);

			try {
				return func(session, instance);

			} catch (ObjectNotFoundException ex) {
				var message = string.Format("Could not find {0} with id {1}", ex.EntityName, ex.Identifier);
				throw new NotFoundException(message, ex);

			} catch (Exception ex) {
				Holder.FailSession(session);
				throw new ActiveRecordException("Error performing Execute for " + type.Name, ex);

			} finally {
				Holder.ReleaseSession(session);
			}
		}

		/// <summary>
		/// Invokes the specified delegate passing a valid 
		/// NHibernate session. Used for custom NHibernate queries.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="action">The delegate instance</param>
		public static void Execute(Type type, Action<ISession> action) {
			Execute(type, session => {
				action(session);
				return string.Empty;
			});
		}

		/// <summary>
		/// Invokes the specified delegate passing a valid 
		/// NHibernate session. Used for custom NHibernate queries.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="func">The delegate instance</param>
		/// <returns>Whatever is returned by the delegate invocation</returns>
		public static TK Execute<TK>(Type type, Func<ISession, TK> func) {
			return Execute<object, TK>(type, (session, arg2) => func(session), null);
		}

		/// <summary>
		/// Invokes the specified delegate passing a valid 
		/// NHibernate stateless session. Used for custom NHibernate queries.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="func">The delegate instance</param>
		/// <param name="instance">The ActiveRecord instance</param>
		/// <returns>Whatever is returned by the delegate invocation</returns>
		public static TK ExecuteStateless<TK>(Type type, Func<IStatelessSession, object, TK> func, object instance) {
			if (func == null) throw new ArgumentNullException("func", "Delegate must be passed");
			if (type == null) throw new ArgumentNullException("type", "Type must be passed");

			EnsureInitialized(type);

			var session = Holder.GetSessionFactory(type).OpenStatelessSession();
			var tx = session.BeginTransaction();
			try {
				var result = func(session, instance);
				tx.Commit();
				return result;
			} catch (Exception ex) {
				tx.Rollback();
				throw new ActiveRecordException("Error performing Execute for " + type.Name, ex);

			} finally {
				tx.Dispose();
				session.Dispose();
			}
		}

		/// <summary>
		/// Invokes the specified delegate passing a valid 
		/// NHibernate stateless session. Used for custom NHibernate queries.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="func">The delegate instance</param>
		/// <returns>Whatever is returned by the delegate invocation</returns>
		public static TK ExecuteStateless<TK>(Type type, Func<IStatelessSession, TK> func) {
			return ExecuteStateless<TK>(type, (session, arg2) => func(session), null);
		}

		/// <summary>
		/// Invokes the specified delegate passing a valid 
		/// NHibernate stateless session. Used for custom NHibernate queries.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="action">The delegate instance</param>
		public static void ExecuteStateless(Type type, Action<IStatelessSession> action) {
			ExecuteStateless<object>(type, session => {
				action(session);
				return string.Empty;
			});
		}

		#endregion

		#region Find/Peek

		/// <summary>
		/// Finds an object instance by its primary key.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="id">ID value</param>
		public static object Find(Type type, object id)
		{
			return Execute<object>(type, session => session.Get(type, id));
		}

		/// <summary>
		/// Peeks for an object instance by its primary key,
		/// returns null if not found
		/// </summary>
		/// <param name="type"></param>
		/// <param name="id">ID value</param>
		public static object Peek(Type type, object id)
		{
			return Execute<object>(type, session => session.Load(type, id));
		}

		#endregion

	}
}
