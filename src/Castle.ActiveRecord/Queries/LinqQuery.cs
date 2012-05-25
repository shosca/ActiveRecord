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


using NHibernate.Criterion;

namespace Castle.ActiveRecord.Linq
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq.Expressions;

	using NHibernate;
	using NHibernate.Engine;
	using NHibernate.Linq;

	/// <summary>
	/// Linq Active Record Query
	/// </summary>
	public class LinqQuery<T>:IActiveRecordQuery
	{
		private readonly QueryOver<T,T> _queryover;

		public LinqQuery() {
			_queryover = NHibernate.Criterion.QueryOver.Of<T>();
		}

		public QueryOver<T,T> QueryOver {
			get { return _queryover; }
		}

		/// <inheritDoc/>
		public List<T> Result { get; private set; }

		public Type RootType {
			get { return typeof (T); }
		}

		/// <inheritDoc />
		public object Execute(ISession session) {
			var result = _queryover.GetExecutableQueryOver(session);
			if (result is IEnumerable<T>)
				Result = new List<T>(result as IEnumerable<T>);
			return result;
		}

		/// <inheritDoc />
		public IEnumerable Enumerate(ISession session)
		{
			return (IEnumerable)Execute(session);
		}
	}
}
