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

namespace Castle.ActiveRecord.Framework
{
	using System;
	using Castle.Core.Configuration;

	/// <summary>
	/// Abstracts the source of configuration for the framework.
	/// </summary>
	public interface IConfigurationSource
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
		IConfiguration GetConfiguration(string key);

		/// <summary>
		/// Returns all registered configuration keys
		/// </summary>
		/// <returns></returns>
		IEnumerable<string> GetAllConfigurationKeys();

		/// <summary>
		/// Gets a value indicating whether this <see cref="IConfigurationSource"/> produce debug information
		/// </summary>
		/// <value><c>true</c> if debug; otherwise, <c>false</c>.</value>
		bool Debug { get; }

		/// <summary>
		/// Determines the default flushing behaviour of scopes.
		/// </summary>
		DefaultFlushType DefaultFlushType { get; }
	}
}
