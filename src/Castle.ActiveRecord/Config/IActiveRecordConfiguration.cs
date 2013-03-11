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

using System.Collections.Generic;
using System;
using Castle.ActiveRecord.Scopes;
using Castle.Core.Configuration;

namespace Castle.ActiveRecord.Config
{
    /// <summary>
    /// Abstracts the source of configuration for the framework.
    /// </summary>
    public interface IActiveRecordConfiguration 
    {
        /// <summary>
        /// Implementors should return the type that implements
        /// the interface <see cref="IThreadScopeInfo"/>
        /// </summary>
        Type ThreadScopeInfoImplementation { get; set; }

        /// <summary>
        /// Implementors should return the type that implements 
        /// the interface <see cref="ISessionFactoryHolder"/>
        /// </summary>
        Type SessionFactoryHolderImplementation { get; set; }

        /// <summary>
        /// Implementors should return a type that implements
        /// NHibernate.Cfg.INamingStrategy
        /// </summary>
        Type NamingStrategyImplementation { get; set; }

        /// <summary>
        /// Implementors should return an <see cref="IConfiguration"/> 
        /// instance
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        SessionFactoryConfig GetConfiguration(string key);

        /// <summary>
        /// Add a config instance
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        void Add(SessionFactoryConfig config);

        /// <summary>
        /// Returns all registered configuration keys
        /// </summary>
        /// <returns></returns>
        IEnumerable<string> GetAllConfigurationKeys();

        /// <summary>
        /// Gets a value indicating whether this <see cref="IActiveRecordConfiguration"/> produce _debug information
        /// </summary>
        /// <value><c>true</c> if _debug; otherwise, <c>false</c>.</value>
        bool Debug { get; set; }

        /// <summary>
        /// Determines default lazy configuration
        /// </summary>
        bool Lazy { get; set; }

        /// <summary>
        /// Determines default auto-import configuration
        /// </summary>
        bool AutoImport { get; set; }

        /// <summary>
        /// Determines the default flushing behaviour of scopes.
        /// </summary>
        DefaultFlushType DefaultFlushType { get; set; }

        void ForEachConfiguration(Action<SessionFactoryConfig> action);
    }
}
