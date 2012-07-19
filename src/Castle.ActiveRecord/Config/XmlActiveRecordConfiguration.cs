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
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace Castle.ActiveRecord.Config
{
	/// <summary>
	/// Source of configuration based on Xml 
	/// source like files, streams or readers.
	/// </summary>
	public class XmlActiveRecordConfiguration : DefaultActiveRecordConfiguration
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="XmlActiveRecordConfiguration"/> class.
		/// </summary>
		protected XmlActiveRecordConfiguration()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="XmlActiveRecordConfiguration"/> class.
		/// </summary>
		/// <param name="xmlFileName">Name of the XML file.</param>
		public XmlActiveRecordConfiguration(String xmlFileName)
		{
			var doc = new XmlDocument();
			doc.Load(xmlFileName);
			PopulateSource(doc.DocumentElement);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="XmlActiveRecordConfiguration"/> class.
		/// </summary>
		/// <param name="stream">The stream.</param>
		public XmlActiveRecordConfiguration(Stream stream)
		{
			var doc = new XmlDocument();
			doc.Load(stream);
			PopulateSource(doc.DocumentElement);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="XmlActiveRecordConfiguration"/> class.
		/// </summary>
		/// <param name="reader">The reader.</param>
		public XmlActiveRecordConfiguration(TextReader reader)
		{
			var doc = new XmlDocument();
			doc.Load(reader);
			PopulateSource(doc.DocumentElement);
		}

		/// <summary>
		/// Populate this instance with values from the given XML node
		/// </summary>
		protected void PopulateSource(XmlNode section)
		{
			if (section.Attributes == null) return;

			XmlAttribute threadInfoAtt = section.Attributes["threadinfotype"];
			XmlAttribute isDebug = section.Attributes["isDebug"];
			XmlAttribute defaultFlushType = section.Attributes["flush"];
			XmlAttribute autoimportatt = section.Attributes["auto-import"];
			XmlAttribute lazyatt = section.Attributes["default-lazy"];

			SetUpThreadInfoType(threadInfoAtt != null ? threadInfoAtt.Value : String.Empty);

			XmlAttribute sessionfactoryholdertypeAtt =
				section.Attributes["sessionfactoryholdertype"];

			SetUpSessionFactoryHolderType(sessionfactoryholdertypeAtt != null
			                              	?
			                              sessionfactoryholdertypeAtt.Value
			                              	: String.Empty);

			var namingStrategyTypeAtt = section.Attributes["namingstrategytype"];

			SetUpNamingStrategyType(namingStrategyTypeAtt != null ? namingStrategyTypeAtt.Value : String.Empty);

			this
				.Debug(ConvertBool(isDebug))
				.AutoImport(autoimportatt == null || ConvertBool(autoimportatt))
				.Lazy(lazyatt == null || ConvertBool(lazyatt))
				.Flush(defaultFlushType != null ? GetFlushType(defaultFlushType.Value) : DefaultFlushType.Classic)
				;

			PopulateConfigNodes(section);
		}

		private void PopulateConfigNodes(XmlNode section)
		{
			const string configNodeName = "config";

			foreach(XmlNode node in section.ChildNodes)
			{
				if (node.NodeType != XmlNodeType.Element) continue;

				if (!configNodeName.Equals(node.Name))
				{
					var message = String.Format("Unexpected node. Expect '{0}' found '{1}'",
					                               configNodeName, node.Name);

					throw new ConfigurationErrorsException(message);
				}

				var sfconfigname = string.Empty;
				if (node.Attributes != null && node.Attributes.Count != 0)
				{
					var typeNameAtt = node.Attributes["type"];
					if (typeNameAtt != null) {
						if (!string.IsNullOrEmpty(typeNameAtt.Value))
							sfconfigname = typeNameAtt.Value;
					}
					
				}
				var sfconfig = this.CreateConfiguration(sfconfigname);

				if (node.Attributes != null && node.Attributes.Count != 0) {


					var databaseName = node.Attributes["database"] ?? node.Attributes["db"];
					var connectionStringName = node.Attributes["connectionStringName"] ?? node.Attributes["csn"];
					if (databaseName != null && connectionStringName != null)
					{
						SetDefaults(sfconfig, databaseName.Value, connectionStringName.Value);
					}
					else if (databaseName != null || connectionStringName != null)
					{
						var message =
							String.Format(
								"Using short form of configuration requires both 'database' and 'connectionStringName' attributes to be specified.");
						throw new ConfigurationErrorsException(message);
					}
				}

				BuildProperties(sfconfig, node);
			}
		}

		/// <summary>
		/// Sets the default configuration for database specifiend by <paramref name="name"/>.
		/// </summary>
		/// <param name="config"></param>
		/// <param name="name">Name of the database type.</param>
		/// <param name="connectionStringName">name of the connection string specified in connectionStrings configuration section</param>
		/// <returns></returns>
		protected void SetDefaults(SessionFactoryConfig config, string name, string connectionStringName)
		{
			var names = Enum.GetNames(typeof(DatabaseType));
			if (!Array.Exists(names, n => n.Equals(name, StringComparison.OrdinalIgnoreCase)))
			{
				var builder = new StringBuilder();
				builder.AppendFormat("Specified value ({0}) is not valid for 'database' attribute. Valid values are:", name);
				foreach (var value in Enum.GetValues(typeof(DatabaseType)))
				{
					builder.AppendFormat(" '{0}'", value.ToString());
				}

				builder.Append(".");
				throw new ConfigurationErrorsException(builder.ToString());
			}

			var type = (DatabaseType)Enum.Parse(typeof(DatabaseType), name, true);
			config.Set(NHibernate.Cfg.Environment.ConnectionStringName, connectionStringName)
				.SetDatabaseType(type);
		}

		/// <summary>
		/// Builds the configuration properties.
		/// </summary>
		/// <param name="config"></param>
		/// <param name="node"> </param>
		/// <returns></returns>
		protected void BuildProperties(SessionFactoryConfig config, XmlNode node)
		{
			foreach(XmlNode addNode in node.SelectNodes("add"))
			{
				var keyAtt = addNode.Attributes["key"];
				var valueAtt = addNode.Attributes["value"];

				if (keyAtt == null || valueAtt == null)
				{
					var message = String.Format("For each 'add' element you must specify 'key' and 'value' attributes");

					throw new ConfigurationErrorsException(message);
				}
				var name = keyAtt.Value;
				var value = valueAtt.Value;

				if (name.Equals("assembly")) {
					config.Assemblies.Add(Assembly.Load(value));
				} else {
					config.Properties[name] = value;
				}
			}
		}

		protected DefaultFlushType GetFlushType(string configurationValue)
		{
			try
			{
				return (DefaultFlushType) Enum.Parse(typeof(DefaultFlushType), configurationValue, true);
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
		/// Sets the type of the thread info.
		/// </summary>
		/// <param name="customType">The type of the custom implementation.</param>
		protected void SetUpThreadInfoType(string customType)
		{
			Type threadInfoType = null;

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


		private static bool ConvertBool(XmlNode boolAttrib)
		{
			return boolAttrib != null && "true".Equals(boolAttrib.Value, StringComparison.OrdinalIgnoreCase);
		}
	}
}
