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

using NHibernate.Cfg;

namespace Castle.ActiveRecord
{
	/// <summary>
	/// <para>
	/// Contributors are an extension point of ActiveRecord. Instances of INHContributor
	/// are registered at <see cref="ActiveRecord"/> before the framework is
	/// initialized. They are called before the session factory is created and can therefore
	/// contribute to NHibernate's configuration of the session factory.
	/// </para>
	/// </summary>
	public interface INHContributor
	{
		/// <summary>
		/// Called to modify the configuration before the session factory is called.
		/// </summary>
		/// <remarks>
		/// The order in which multiple contributors are called is not determined. The method
		/// must not assume any fixed order and must therefore not be used to counter 
		/// modifications by other contributors. 
		/// </remarks>
		/// <param name="configuration">The NH configuration to modify.</param>
		void Contribute(Configuration configuration);
	}
}