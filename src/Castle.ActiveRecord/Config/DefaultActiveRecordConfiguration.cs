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
using Castle.Core.Configuration;

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
            AutoImport = true;
            Lazy = true;
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
        /// Return a type that implements
        /// NHibernate.Cfg.INamingStrategy
        /// </summary>
        public Type NamingStrategyImplementation { get; set; }

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
        /// Determines default lazy configuration
        /// </summary>
        public bool Lazy { get; set; }

        /// <summary>
        /// Determines default auto-import configuration
        /// </summary>
        public bool AutoImport { get; set; }

        /// <summary>
        /// Determines the default flushing behaviour of scopes.
        /// </summary>
        public DefaultFlushType DefaultFlushType { get; set; }

        public void ForEachConfiguration(Action<SessionFactoryConfig> action) {
            foreach (var key in GetAllConfigurationKeys()) {
                action(GetConfiguration(key));
            }
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
    }
}
