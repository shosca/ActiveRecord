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
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using Castle.ActiveRecord.ByteCode;
using Castle.Core.Configuration;
using NHibernate.Connection;
using Environment = NHibernate.Cfg.Environment;

namespace Castle.ActiveRecord.Config
{
	/// <summary>
	/// Useful for test cases.
	/// </summary>
	public class DefaultActiveRecordConfiguration : IActiveRecordConfiguration
	{
		readonly IDictionary<string, SessionFactoryConfig> _configs = new Dictionary<string, SessionFactoryConfig>();

		/// <summary>
		/// Initializes a new instance of the <see cref="DefaultActiveRecordConfiguration"/> class.
		/// </summary>
		public DefaultActiveRecordConfiguration() {
			DefaultFlushType = DefaultFlushType.Classic;
		}

		/// <summary>
		/// Return a type that implements
		/// the interface <see cref="IThreadScopeInfo"/>
		/// </summary>
		/// <value></value>
		public Type ThreadScopeInfoImplementation { get; set; }

		/// <summary>
		/// Return a type that implements
		/// the interface <see cref="ISessionFactoryHolder"/>
		/// </summary>
		/// <value></value>
		public Type SessionFactoryHolderImplementation { get; set; }

		/// <summary>
		/// Return an <see cref="IConfiguration"/> for the specified type.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public SessionFactoryConfig GetConfiguration(string key)
		{
			key = string.IsNullOrEmpty(key) ? string.Empty : key;
			SessionFactoryConfig configuration;
			_configs.TryGetValue(key, out configuration);
			return configuration;
		}

		public IEnumerable<string> GetAllConfigurationKeys() {
			return _configs.Keys;
		}

		/// <summary>
		/// Gets a value indicating whether this <see cref="IActiveRecordConfiguration"/> produces _debug information.
		/// </summary>
		/// <value><c>true</c> if _debug; otherwise, <c>false</c>.</value>
		public bool Debug { get; set; }

		/// <summary>
		/// Determines the default flushing behaviour of scopes.
		/// </summary>
		public DefaultFlushType DefaultFlushType { get; set; }

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
		/// Adds the specified type with configuration
		/// </summary>
		/// <param name="config">The config.</param>
		public void Add(SessionFactoryConfig config)
		{
			var key = string.IsNullOrEmpty(config.Name) ? string.Empty : config.Name;
			_configs.Add(key, config);
		}

		/// <summary>
		/// Sets the type of the thread info.
		/// </summary>
		/// <param name="isWeb">If we are running in a web context.</param>
		/// <param name="customType">The type of the custom implementation.</param>
		protected void SetUpThreadInfoType(bool isWeb, string customType)
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
		/// Sets the _debug flag.
		/// </summary>
		/// <param name="isDebug">If set to <c>true</c> ActiveRecord will produce _debug information.</param>
		public void SetDebugFlag(bool isDebug)
		{
			Debug = isDebug;
		}

		/// <summary>
		/// Sets the value indicating the default flush behaviour.
		/// </summary>
		/// <param name="flushType">The chosen default behaviour.</param>
		protected void SetDefaultFlushType(DefaultFlushType flushType)
		{
			DefaultFlushType = flushType;
		}

		/// <summary>
		/// Sets the default flushing behaviour using the string value from the configuration
		/// XML. This method has been moved from XmlActiveRecordConfiguration to avoid code
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

		/// <summary>
		/// Sets the flush behaviour for <cref>ISessionScope</cref> when no
		/// other behaviour is specified in the scope itself. The default for
		/// this configuration is <cref>DefaultFlushType.Classic</cref>. See
		/// <see cref="DefaultFlushType"/> for what the options mean.
		/// </summary>
		/// <param name="flushType">The default flushing behaviour to set.</param>
		/// <returns>The fluent configuration itself.</returns>
		public IActiveRecordConfiguration Flush(DefaultFlushType flushType)
		{
			DefaultFlushType = flushType;
			return this;
		}

		/// <summary>
		/// Sets the <see cref="IThreadScopeInfo"/> to use. Normally, this type is
		/// set when ActiveRecord is used in web application. You should set this
		/// value only if you need a custom implementation of that interface.
		/// </summary>
		/// <typeparam name="T">The implementation to use.</typeparam>
		/// <returns>The fluent configuration itself.</returns>
		public IActiveRecordConfiguration UseThreadScopeInfo<T>() where T : IThreadScopeInfo
		{
			ThreadScopeInfoImplementation = typeof (T);
			return this;
		}

		/// <summary>
		/// Sets the <see cref="ISessionFactoryHolder"/> to use. You should set this if you need to
		/// use a custom implementation of that interface.
		/// </summary>
		/// <typeparam name="T">The implementation to use.</typeparam>
		/// <returns>The fluent configuration itself.</returns>
		public IActiveRecordConfiguration UseSessionFactoryHolder<T>() where T : ISessionFactoryHolder
		{
			SessionFactoryHolderImplementation = typeof (T);
			return this;
		}
	}
}
