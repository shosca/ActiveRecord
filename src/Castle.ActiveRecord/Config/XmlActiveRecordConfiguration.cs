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
			XmlDocument doc = new XmlDocument();
			doc.Load(xmlFileName);
			PopulateSource(doc.DocumentElement);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="XmlActiveRecordConfiguration"/> class.
		/// </summary>
		/// <param name="stream">The stream.</param>
		public XmlActiveRecordConfiguration(Stream stream)
		{
			XmlDocument doc = new XmlDocument();
			doc.Load(stream);
			PopulateSource(doc.DocumentElement);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="XmlActiveRecordConfiguration"/> class.
		/// </summary>
		/// <param name="reader">The reader.</param>
		public XmlActiveRecordConfiguration(TextReader reader)
		{
			XmlDocument doc = new XmlDocument();
			doc.Load(reader);
			PopulateSource(doc.DocumentElement);
		}

		/// <summary>
		/// Populate this instance with values from the given XML node
		/// </summary>
		protected void PopulateSource(XmlNode section)
		{
			if (section.Attributes == null) return;

			XmlAttribute isWebAtt = section.Attributes["isWeb"];
			XmlAttribute threadInfoAtt = section.Attributes["threadinfotype"];
			XmlAttribute isDebug = section.Attributes["isDebug"];
			XmlAttribute defaultFlushType = section.Attributes["flush"];

			SetUpThreadInfoType(isWebAtt != null && "true" == isWebAtt.Value,
			                    threadInfoAtt != null ? threadInfoAtt.Value : String.Empty);

			XmlAttribute sessionfactoryholdertypeAtt =
				section.Attributes["sessionfactoryholdertype"];

			SetUpSessionFactoryHolderType(sessionfactoryholdertypeAtt != null
			                              	?
			                              sessionfactoryholdertypeAtt.Value
			                              	: String.Empty);

			XmlAttribute namingStrategyTypeAtt = section.Attributes["namingstrategytype"];

			SetUpNamingStrategyType(namingStrategyTypeAtt != null ? namingStrategyTypeAtt.Value : String.Empty);

			SetDebugFlag(ConvertBool(isDebug));

			if (defaultFlushType == null)
			{
				SetDefaultFlushType(DefaultFlushType.Classic);
			}
			else
			{
				SetDefaultFlushType(defaultFlushType.Value);
			}

			PopulateConfigNodes(section);
		}

		private void PopulateConfigNodes(XmlNode section)
		{
			const string Config_Node_Name = "config";

			foreach(XmlNode node in section.ChildNodes)
			{
				if (node.NodeType != XmlNodeType.Element) continue;

				if (!Config_Node_Name.Equals(node.Name))
				{
					String message = String.Format("Unexpected node. Expect '{0}' found '{1}'",
					                               Config_Node_Name, node.Name);

					throw new ConfigurationErrorsException(message);
				}

				SessionFactoryConfig sfconfig = null;

				string sfconfigname = string.Empty;
				if (node.Attributes != null && node.Attributes.Count != 0) {

					XmlAttribute typeNameAtt = node.Attributes["type"];
					if (typeNameAtt != null) {
						if (!string.IsNullOrEmpty(typeNameAtt.Value))
							sfconfigname = typeNameAtt.Value;
					}

					var databaseName = node.Attributes["database"] ?? node.Attributes["db"];
					var connectionStringName = node.Attributes["connectionStringName"] ?? node.Attributes["csn"];
					if (databaseName != null && connectionStringName != null)
					{
						sfconfig = SetDefaults(databaseName.Value, connectionStringName.Value);
					}
					else if (databaseName != null || connectionStringName != null)
					{
						var message =
							String.Format(
								"Using short form of configuration requires both 'database' and 'connectionStringName' attributes to be specified.");
						throw new ConfigurationErrorsException(message);
					}
				}
				if (sfconfig == null)
					sfconfig = new SessionFactoryConfig(this);

				sfconfig.Name = sfconfigname; 
				BuildProperties(sfconfig, node);

				Add(sfconfig);
			}
		}

		/// <summary>
		/// Sets the default configuration for database specifiend by <paramref name="name"/>.
		/// </summary>
		/// <param name="name">Name of the database type.</param>
		/// <param name="connectionStringName">name of the connection string specified in connectionStrings configuration section</param>
		/// <returns></returns>
		protected SessionFactoryConfig SetDefaults(string name, string connectionStringName)
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
			var defaults = this.CreateConfiguration(type)
							.Set(NHibernate.Cfg.Environment.ConnectionStringName, connectionStringName);
			return defaults;
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
				XmlAttribute keyAtt = addNode.Attributes["key"];
				XmlAttribute valueAtt = addNode.Attributes["value"];

				if (keyAtt == null || valueAtt == null)
				{
					String message = String.Format("For each 'add' element you must specify 'key' and 'value' attributes");

					throw new ConfigurationErrorsException(message);
				}
				string name = keyAtt.Value;
				string value = valueAtt.Value;

				if (name.Equals("assembly")) {
					config.Assemblies.Add(Assembly.Load(value));
				} else {
					config.Properties[name] = value;
				}
			}
		}

		private static bool ConvertBool(XmlNode boolAttrib)
		{
			return boolAttrib != null && "true".Equals(boolAttrib.Value, StringComparison.OrdinalIgnoreCase);
		}
	}
}
