using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Castle.ActiveRecord.Framework;
using Castle.ActiveRecord.Framework.Config;
using Castle.ActiveRecord.Scopes;
using Castle.Core.Configuration;
using Iesi.Collections.Generic;
using NHibernate.Cfg;
using NHibernate.Cfg.MappingSchema;
using NHibernate.Mapping.ByCode;
using NHibernate.Tool.hbm2ddl;
using Environment = NHibernate.Cfg.Environment;

namespace Castle.ActiveRecord {
	/// <summary>
	/// Performs the framework initialization.
	/// </summary>
	/// <remarks>
	/// This class is not thread safe.
	/// </remarks>
	public static class ActiveRecord
	{
		private static readonly Object lockConfig = new object();

		public static IConfigurationSource ConfigurationSource { get; private set; }

		/// <summary>
		/// The global holder for the session factories.
		/// </summary>
		public static ISessionFactoryHolder Holder { get; private set; }

		/// <summary>
		/// So others frameworks can intercept the 
		/// creation and act on the holder instance
		/// </summary>
		public static event SessionFactoryHolderDelegate SessionFactoryHolderCreated;

		/// <summary>
		/// Allows other frameworks to modify the ActiveRecordModel
		/// before the generation of the NHibernate configuration.
		/// As an example, this may be used to rewrite table names to
		/// conform to an application-specific standard.  Since the
		/// configuration source is passed in, it is possible to
		/// determine the underlying database type and make changes
		/// if necessary.
		/// </summary>
		public static event MapperDelegate MapperCreated;

		/// <summary>
		/// Allows other frameworks to modify the ActiveRecordModel
		/// before the generation of the NHibernate configuration.
		/// As an example, this may be used to rewrite table names to
		/// conform to an application-specific standard.  Since the
		/// configuration source is passed in, it is possible to
		/// determine the underlying database type and make changes
		/// if necessary.
		/// </summary>
		public static event MapperDelegate AfterMappingsAdded;


		/// <summary>
		/// Initialize the mappings using the configuration and 
		/// checking all the types on the specified Assemblies
		/// </summary>
		public static void Initialize(IConfigurationSource source)
		{
			CreateSessionFactoryAndRegisterToHolder(source);
		}

		/// <summary>
		/// Initializes the framework reading the configuration from
		/// the <c>AppDomain</c> and checking all the types on the executing <c>Assembly</c>
		/// </summary>
		public static void Initialize()
		{
			IConfigurationSource source = ActiveRecordSectionHandler.Instance;

			Initialize(source);
		}

		static bool IsClassMapperType(Type t) {
			return t.Name.ToLower().EndsWith("mapping");
		}

		/// <summary>
		/// Initialize the mappings using the configuration and 
		/// the list of types
		/// </summary>
		static void CreateSessionFactoryAndRegisterToHolder(IConfigurationSource source)
		{
			if (source == null)
			{
				throw new ArgumentNullException("source");
			}

			lock(lockConfig)
			{
				if (Holder == null) {
					// First initialization
					Holder = CreateSessionFactoryHolderImplementation(source);
					Holder.ThreadScopeInfo = CreateThreadScopeInfoImplementation(source);
					RaiseSessionFactoryHolderCreated(Holder);
				}

				ConfigurationSource = source;

				foreach (var key in source.GetAllConfigurationKeys()) {
					ConventionModelMapper mapper = new ConventionModelMapper();
					if (MapperCreated != null)
						MapperCreated(mapper, source);

					var config = source.GetConfiguration(key);
					var assemblies = config.Children.Where(c => c.Name.Equals("assembly")).Select(c => Assembly.Load(c.Value)).ToArray();
					var mappingtypes = assemblies.SelectMany(a => a.GetExportedTypes()).Where(t => IsClassMapperType(t)).ToArray();
					if (assemblies.Length < 1 || mappingtypes.Length < 1) {
						throw new ActiveRecordException("No assembly defined in configuration that contains " +
						                                "mappings.");
					}
					mapper.AddMappings(mappingtypes);

					if (AfterMappingsAdded != null)
						AfterMappingsAdded(mapper, source);

					var mapping = mapper.CompileMappingForAllExplicitlyAddedEntities();
					var cfg = CreateConfiguration(config);
					cfg.DataBaseIntegration(db => {
						db.LogSqlInConsole = source.Debug;
						db.LogFormattedSql = source.Debug;
					});
					cfg.AddMapping(mapping);
					Holder.RegisterConfiguration(cfg);
				}

			}
		}

		/// <summary>
		/// Generates and executes the creation scripts for the database.
		/// </summary>
		public static void CreateSchema()
		{
			CheckInitialized();

			foreach(Configuration config in Holder.GetAllConfigurations())
			{
				SchemaExport export = CreateSchemaExport(config);

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

			Configuration config = Holder.GetConfiguration(baseClass);

			SchemaExport export = CreateSchemaExport(config);

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

			foreach(Configuration config in Holder.GetAllConfigurations())
			{
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
			List<Exception> exceptions = new List<Exception>();

			foreach(Configuration config in Holder.GetAllConfigurations())
			{
				SchemaUpdate updater = CreateSchemaUpdate(config);

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

			Configuration config = Holder.GetConfiguration(baseClass);

			SchemaUpdate updater = CreateSchemaUpdate(config);

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

			bool isFirstExport = true;
			int fileCount = 1;

			foreach(Configuration config in Holder.GetAllConfigurations())
			{
				SchemaExport export = CreateSchemaExport(config);

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

			Configuration config = Holder.GetConfiguration(baseType);

			SchemaExport export = CreateSchemaExport(config);

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

			bool isFirstExport = true;
			int fileCount = 1;

			foreach(Configuration config in Holder.GetAllConfigurations())
			{
				SchemaExport export = CreateSchemaExport(config);

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

			Configuration config = Holder.GetConfiguration(baseType);

			SchemaExport export = CreateSchemaExport(config);

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
		public static void ResetInitializationFlag()
		{
			// Make sure we start with it enabled
			Environment.UseReflectionOptimizer = true;
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
			SchemaExport export = new SchemaExport(cfg);
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

		private static Configuration CreateConfiguration(IConfiguration config)
		{

			Configuration cfg = new Configuration();

			foreach(IConfiguration childConfig in config.Children.Where(c => !c.Name.Equals("assembly")))
			{
				cfg.Properties[childConfig.Name] = childConfig.Value;
			}

			return cfg;
		}

		private static void RaiseSessionFactoryHolderCreated(ISessionFactoryHolder holder)
		{
			if (SessionFactoryHolderCreated != null)
			{
				SessionFactoryHolderCreated(holder);
			}
		}

		private static ISessionFactoryHolder CreateSessionFactoryHolderImplementation(IConfigurationSource source)
		{
			if (source.SessionFactoryHolderImplementation != null)
			{
				Type sessionFactoryHolderType = source.SessionFactoryHolderImplementation;

				if (!typeof(ISessionFactoryHolder).IsAssignableFrom(sessionFactoryHolderType))
				{
					String message =
						String.Format("The specified type {0} does " + "not implement the interface ISessionFactoryHolder",
						              sessionFactoryHolderType.FullName);

					throw new ActiveRecordException(message);
				}

				return (ISessionFactoryHolder) Activator.CreateInstance(sessionFactoryHolderType);
			}
			else
			{
				return new SessionFactoryHolder();
			}
		}

		private static IThreadScopeInfo CreateThreadScopeInfoImplementation(IConfigurationSource source)
		{
			if (source.ThreadScopeInfoImplementation != null)
			{
				Type threadScopeType = source.ThreadScopeInfoImplementation;

				if (!typeof(IThreadScopeInfo).IsAssignableFrom(threadScopeType))
				{
					String message =
						String.Format("The specified type {0} does " + "not implement the interface IThreadScopeInfo",
						              threadScopeType.FullName);

					throw new ActiveRecordInitializationException(message);
				}

				return (IThreadScopeInfo) Activator.CreateInstance(threadScopeType);
			}
			else
			{
				return new ThreadScopeInfo();
			}
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
			string path = Path.GetDirectoryName(originalFileName);
			string fileName = Path.GetFileNameWithoutExtension(originalFileName);
			string extension = Path.GetExtension(originalFileName);

			return Path.Combine(path, string.Format("{0}_{1}{2}", fileName, fileCount, extension));
		}
	}
}
