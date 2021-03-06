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
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Web;

namespace Castle.ActiveRecord.Scopes
{
	/// <summary>
	/// This <see cref="IThreadScopeInfo"/> implementation will first try to get the current scope from the current 
	/// request, and if not found, will use a thread lcoal scope.
	/// </summary>
	/// <remarks>
	/// This is used for scenarios where most of the you need per request scope, but you also does some work outside a 
	/// request (in a thread pool thread, for instnace).
	/// </remarks>
	public class HybridWebThreadScopeInfo : AbstractThreadScopeInfo, IWebThreadScopeInfo
	{
		const string ActiveRecordCurrentStack = "activerecord.currentstack";

		[ThreadStatic] static Stack<ISessionScope> stack;

		/// <summary>
		/// Gets the current stack.
		/// </summary>
		/// <value>The current stack.</value>
		public override Stack<ISessionScope> CurrentStack
		{
			[MethodImpl(MethodImplOptions.Synchronized)]
			get
			{
				var current = HttpContext.Current;

				if (current == null)
				{
					return stack ?? (stack = new Stack<ISessionScope>());
				}

				var contextstack = current.Items[ActiveRecordCurrentStack] as Stack<ISessionScope>;

				if (contextstack == null)
				{
					contextstack = new Stack<ISessionScope>();

					current.Items[ActiveRecordCurrentStack] = contextstack;
				}

				return contextstack;
			}
		}
	}
}
