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

using Castle.ActiveRecord.Scopes;
using System;
using System.Web;

namespace Castle.ActiveRecord
{
	/// <summary>
	/// HttpModule to set up a session for the request lifetime.
	/// <seealso cref="SessionScope"/>
	/// </summary>
	/// <remarks>
	/// To install the module, you must:
	/// <para>
	///    <list type="number">
	///      <item>
	///        <description>
	///        Add the module to the <c>httpModules</c> configuration section within <c>system.web</c>
	///        </description>
	///      </item>
	///    </list>
	/// </para>
	/// </remarks>
	public class SessionScopeWebModule : IHttpModule
	{
		/// <summary>
		/// The key used to store the session in the context items
		/// </summary>
		protected static readonly String SessionKey = "SessionScopeWebModule.session";
		
		/// <summary>
		/// Initialize the module.
		/// </summary>
		/// <param name="app">The app.</param>
		public void Init(HttpApplication app)
		{
			app.BeginRequest += OnBeginRequest;
			app.EndRequest += OnEndRequest;
		}

		/// <summary>
		/// Disposes of the resources (other than memory) used by the module that implements <see cref="T:System.Web.IHttpModule"></see>.
		/// </summary>
		public void Dispose()
		{
		}

		const string Misconfigerrmessage = "Seems that the framework isn't configured properly. " +
											"(SessionScopeWebModule is not use) " +
											"Check the documentation for further information";

		/// <summary>
		/// Called when request is started, create a session for the request
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		private void OnBeginRequest(object sender, EventArgs e)
		{
			if (!AR.IsInitialized) return;

			var app = sender as HttpApplication;
			if (app == null || !(AR.Holder.ThreadScopeInfo is IWebThreadScopeInfo))
				throw new ActiveRecordException(Misconfigerrmessage);

			app.Context.Items.Add(SessionKey, new SessionScope());
		}

		/// <summary>
		/// Called when the request ends, dipose of the scope
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		private void OnEndRequest(object sender, EventArgs e)
		{
			if (!AR.IsInitialized) return;

			var app = sender as HttpApplication;
			if (app == null || !(AR.Holder.ThreadScopeInfo is IWebThreadScopeInfo))
				throw new ActiveRecordException(Misconfigerrmessage);

			var session = app.Context.Items[SessionKey] as ISessionScope;
			if (session != null) session.Dispose();
		}
	}
}
