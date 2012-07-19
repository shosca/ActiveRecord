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
		Type ThreadScopeInfoImplementation { get; }

		/// <summary>
		/// Implementors should return the type that implements 
		/// the interface <see cref="ISessionFactoryHolder"/>
		/// </summary>
		Type SessionFactoryHolderImplementation { get; }

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
		bool Debug { get; }

		/// <summary>
		/// Determines default lazy configuration
		/// </summary>
		bool Lazy { get; }

		/// <summary>
		/// Determines default auto-import configuration
		/// </summary>
		bool AutoImport { get; }

		/// <summary>
		/// Determines the default flushing behaviour of scopes.
		/// </summary>
		DefaultFlushType DefaultFlushType { get; }

		/// <summary>
		/// Sets the flush behaviour for <see cref="ISessionScope"/> when no
		/// other behaviour is specified in the scope itself. The default for
		/// this configuration is <cref>DefaultFlushType.Classic</cref>. See
		/// <see cref="DefaultFlushType"/> for what the options mean.
		/// </summary>
		/// <param name="flushType">The default flushing behaviour to set.</param>
		/// <returns>The fluent configuration itself.</returns>
		IActiveRecordConfiguration Flush(DefaultFlushType flushType);

		/// <summary>
		/// Sets the <see cref="IThreadScopeInfo"/> to use. Normally, this type is
		/// set when ActiveRecord is used in web application. You should set this
		/// value only if you need a custom implementation of that interface.
		/// </summary>
		/// <typeparam name="T">The implementation to use.</typeparam>
		/// <returns>The fluent configuration itself.</returns>
		IActiveRecordConfiguration UseThreadScopeInfo<T>() where T : IThreadScopeInfo;

		/// <summary>
		/// Sets the <see cref="ISessionFactoryHolder"/> to use. You should set this if you need to
		/// use a custom implementation of that interface.
		/// </summary>
		/// <typeparam name="T">The implementation to use.</typeparam>
		/// <returns>The fluent configuration itself.</returns>
		IActiveRecordConfiguration UseSessionFactoryHolder<T>() where T : ISessionFactoryHolder;
	}
}
