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

namespace Castle.ActiveRecord.Framework.Config
{
	using System;
	using System.Collections.Generic;
	using System.Configuration;
	using System.Text.RegularExpressions;
	using Castle.Core.Configuration;

	/// <summary>
	/// Usefull for test cases.
	/// </summary>
	public class InPlaceConfigurationSource : IConfigurationSource
	{
		private readonly IDictionary<string, IConfiguration> _type2Config = new Dictionary<string, IConfiguration>();
		private Type threadScopeInfoImplementation;
		private Type sessionFactoryHolderImplementation;
		private bool debug;
		private DefaultFlushType defaultFlushType = DefaultFlushType.Classic;

		/// <summary>
		/// Initializes a new instance of the <see cref="InPlaceConfigurationSource"/> class.
		/// </summary>
		public InPlaceConfigurationSource()
		{
		}

		#region IConfigurationSource Members

		/// <summary>
		/// Return a type that implements
		/// the interface <see cref="IThreadScopeInfo"/>
		/// </summary>
		/// <value></value>
		public Type ThreadScopeInfoImplementation
		{
			get { return threadScopeInfoImplementation; }
			set { threadScopeInfoImplementation = value; }
		}

		/// <summary>
		/// Return a type that implements
		/// the interface <see cref="ISessionFactoryHolder"/>
		/// </summary>
		/// <value></value>
		public Type SessionFactoryHolderImplementation
		{
			get { return sessionFactoryHolderImplementation; }
			set { sessionFactoryHolderImplementation = value; }
		}

		/// <summary>
		/// Return an <see cref="IConfiguration"/> for the specified type.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public IConfiguration GetConfiguration(string key)
		{
			key = string.IsNullOrEmpty(key) ? string.Empty : key;
			IConfiguration configuration;
			_type2Config.TryGetValue(key, out configuration);
			return configuration;
		}

		public IEnumerable<string> GetAllConfigurationKeys() {
			return _type2Config.Keys;
		}

		/// <summary>
		/// Gets a value indicating whether this <see cref="IConfigurationSource"/> produces debug information.
		/// </summary>
		/// <value><c>true</c> if debug; otherwise, <c>false</c>.</value>
		public bool Debug
		{
			get { return debug; }
		}

		/// <summary>
		/// Determines the default flushing behaviour of scopes.
		/// </summary>
		public DefaultFlushType DefaultFlushType
		{
			get { return defaultFlushType; }
			set { defaultFlushType = value; }
		}

		/// <summary>
		/// When <c>true</c>, NHibernate.Search event listeners are added.
		/// </summary>
		public virtual bool Searchable { get; set; }

		#endregion

		/// <summary>
		/// Builds a InPlaceConfigurationSource set up to access a MS SQL server database using integrated security.
		/// </summary>
		/// <param name="server">The server.</param>
		/// <param name="initialCatalog">The initial catalog.</param>
		/// <returns></returns>
		public static InPlaceConfigurationSource BuildForMSSqlServer(string server, string initialCatalog)
		{
			if (string.IsNullOrEmpty(server)) throw new ArgumentNullException("server");
			if (string.IsNullOrEmpty(initialCatalog)) throw new ArgumentNullException("initialCatalog");

			return Build(DatabaseType.MsSqlServer2005, "Server=" + server + ";initial catalog=" + initialCatalog + ";Integrated Security=SSPI");
		}

		/// <summary>
		/// Builds a InPlaceConfigurationSource set up to access a MS SQL server database using the specified username and password.
		/// </summary>
		/// <param name="server">The server.</param>
		/// <param name="initialCatalog">The initial catalog.</param>
		/// <param name="username">The username.</param>
		/// <param name="password">The password.</param>
		/// <returns></returns>
		public static InPlaceConfigurationSource BuildForMSSqlServer(string server, string initialCatalog, string username, string password)
		{
			if (string.IsNullOrEmpty(server)) throw new ArgumentNullException("server");
			if (string.IsNullOrEmpty(initialCatalog)) throw new ArgumentNullException("initialCatalog");
			if (string.IsNullOrEmpty(username)) throw new ArgumentNullException("username");
			if (string.IsNullOrEmpty(password)) throw new ArgumentNullException("password");

			return Build(DatabaseType.MsSqlServer2005, "Server=" + server + ";initial catalog=" + initialCatalog + ";User id=" + username + ";password=" + password);
		}

		/// <summary>
		/// Builds an <see cref="InPlaceConfigurationSource"/> for the specified database.
		/// </summary>
		/// <param name="database">The database type.</param>
		/// <param name="connectionString">The connection string.</param>
		/// <returns></returns>
		public static InPlaceConfigurationSource Build(DatabaseType database, string connectionString)
		{
			if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException("connectionString");

			var config = new InPlaceConfigurationSource();

			var parameters = new DefaultDatabaseConfiguration().For(database);
			parameters["connection.connection_string"] = connectionString;
			config.Add(string.Empty, parameters);

			return config;
		}

		/// <summary>
		/// Sets a value indicating whether this instance is running in web app.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is running in web app; otherwise, <c>false</c>.
		/// </value>
		public bool IsRunningInWebApp
		{
			set { SetUpThreadInfoType(value, null); }
		}

		/// <summary>
		/// Adds the specified type with the properties
		/// </summary>
		/// <param name="key">The type.</param>
		/// <param name="properties">The properties.</param>
		public void Add(string key, IDictionary<string,string> properties)
		{
			Add(key, ConvertToConfiguration(properties));
		}

		/// <summary>
		/// Adds the specified type with configuration
		/// </summary>
		/// <param name="key">The type.</param>
		/// <param name="config">The config.</param>
		public void Add(string key, IConfiguration config)
		{
			key = string.IsNullOrEmpty(key) ? string.Empty : key;
			ProcessConfiguration(config);

			_type2Config[key] = config;
		}

		/// <summary>
		/// Sets the type of the thread info.
		/// </summary>
		/// <param name="isWeb">If we are running in a web context.</param>
		/// <param name="customType">The type of the custom implementation.</param>
		protected void SetUpThreadInfoType(bool isWeb, String customType)
		{
			Type threadInfoType = null;

			if (isWeb)
			{
				threadInfoType = Type.GetType("Castle.ActiveRecord.Scopes.WebThreadScopeInfo, Castle.ActiveRecord.Web");
			}

			if (!string.IsNullOrEmpty(customType))
			{
				String typeName = customType;

				threadInfoType = Type.GetType(typeName, false, false);

				if (threadInfoType == null)
				{
					String message = String.Format("The type name {0} could not be found", typeName);

					throw new ActiveRecordException(message);
				}
			}

			ThreadScopeInfoImplementation = threadInfoType;
		}

		/// <summary>
		/// Sets the type of the session factory holder.
		/// </summary>
		/// <param name="customType">Custom implementation</param>
		protected void SetUpSessionFactoryHolderType(String customType)
		{
			Type sessionFactoryHolderType = typeof(SessionFactoryHolder);

			if (!string.IsNullOrEmpty(customType))
			{
				String typeName = customType;

				sessionFactoryHolderType = Type.GetType(typeName, false, false);

				if (sessionFactoryHolderType == null)
				{
					String message = String.Format("The type name {0} could not be found", typeName);

					throw new ActiveRecordException(message);
				}
			}

			SessionFactoryHolderImplementation = sessionFactoryHolderType;
		}

		/// <summary>
		/// Sets the type of the naming strategy.
		/// </summary>
		/// <param name="customType">Custom implementation type name.</param>
		protected void SetUpNamingStrategyType(String customType)
		{
			if (!string.IsNullOrEmpty(customType))
			{
				String typeName = customType;

				Type namingStrategyType = Type.GetType(typeName, false, false);

				if (namingStrategyType == null)
				{
					String message = String.Format("The type name {0} could not be found", typeName);

					throw new ActiveRecordException(message);
				}
			}
		}

		/// <summary>
		/// Sets the debug flag.
		/// </summary>
		/// <param name="isDebug">If set to <c>true</c> ActiveRecord will produce debug information.</param>
		public void SetDebugFlag(bool isDebug)
		{
			debug = isDebug;
		}

		/// <summary>
		/// Sets the value indicating the default flush behaviour.
		/// </summary>
		/// <param name="flushType">The chosen default behaviour.</param>
		protected void SetDefaultFlushType(DefaultFlushType flushType)
		{
			defaultFlushType = flushType;
		}

		/// <summary>
		/// Sets the default flushing behaviour using the string value from the configuration
		/// XML. This method has been moved from XmlConfigurationSource to avoid code
		/// duplication in ActiveRecordIntegrationFacility.
		/// </summary>
		/// <param name="configurationValue">The configuration value.</param>
		protected void SetDefaultFlushType(string configurationValue)
		{
			try
			{
				SetDefaultFlushType((DefaultFlushType) Enum.Parse(typeof(DefaultFlushType), configurationValue, true));
			}
			catch (ArgumentException ex)
			{
				string msg = "Problem: The value of the flush-attribute in <activerecord> is not valid. " +
					"The value was \"" + configurationValue + "\". ActiveRecord expects that value to be one of " +
					string.Join(", ", Enum.GetNames(typeof(DefaultFlushType))) + ". ";

				throw new ConfigurationErrorsException(msg, ex);
			}
		}

		private static IConfiguration ConvertToConfiguration(IDictionary<string,string> properties)
		{
			MutableConfiguration conf = new MutableConfiguration("Config");

			foreach(KeyValuePair<string,string> entry in properties)
			{
				conf.Children.Add(new MutableConfiguration(entry.Key, entry.Value));
			}

			return conf;
		}

		/// <summary>
		/// Processes the configuration applying any substitutions.
		/// </summary>
		/// <param name="config">The configuration to process.</param>
		private static void ProcessConfiguration(IConfiguration config)
		{
			const string ConnectionStringKey = "connection.connection_string";

			for(int i = 0; i < config.Children.Count; ++i)
			{
				IConfiguration property = config.Children[i];

				if (property.Name.IndexOf(ConnectionStringKey) >= 0)
				{
					String value = property.Value;
					Regex connectionStringRegex = new Regex(@"ConnectionString\s*=\s*\$\{(?<ConnectionStringName>[^}]+)\}");

					if (connectionStringRegex.IsMatch(value))
					{
						string connectionStringName = connectionStringRegex.Match(value).
							Groups["ConnectionStringName"].Value;
						value = ConfigurationManager.ConnectionStrings[connectionStringName].ConnectionString;
						config.Children[i] = new MutableConfiguration(property.Name, value);
					}
				}
			}
		}
	}
}
