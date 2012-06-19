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
using System.Linq;
using Castle.Core.Internal;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Impl;

namespace Castle.ActiveRecord {
	public static class QueryExtensions {
		public static IEnumerable<T> List<T>(this QueryOver<T> query) where T : class
		{
			return AR.Execute<T, IEnumerable<T>>(session => query.GetExecutableQueryOver(session).List<T>());
		}

		public static IEnumerable<TR> List<T, TR>(this QueryOver<T> query) where T : class
		{
			return AR.Execute<T, IEnumerable<TR>>(session => query.GetExecutableQueryOver(session).List<TR>());
		}

		public static TR UniqueResult<T, TR>(this QueryOver<T> query) where T : class
		{
			return AR.Execute<T, TR>(session => query.GetExecutableQueryOver(session).List<TR>().FirstOrDefault());
		}

		public static IEnumerable<T> SlicedFindAll<T>(this QueryOver<T> query, int firstResult, int maxResults) where T : class
		{
			return AR.Execute<T, IEnumerable<T>>(session => query.GetExecutableQueryOver(session)
				.Skip(firstResult)
				.Take(maxResults)
				.List<T>());
		}


		public static void DeleteAll<T>(this QueryOver<T, T> query) where T : class
		{
			AR.DeleteAll<T>(query);
		}

		public static IEnumerable<T> List<T>(this DetachedCriteria query) where T : class
		{
			return AR.Execute<T, IEnumerable<T>>(session => query.GetExecutableCriteria(session).List<T>());
		}

		public static IEnumerable<TR> List<T, TR>(this DetachedCriteria query) where T : class
		{
			return AR.Execute<T, IEnumerable<TR>>(session => query.GetExecutableCriteria(session).List<TR>());
		}

		public static TR UniqueResult<T, TR>(this DetachedCriteria query) where T : class
		{
			return AR.Execute<T, TR>(session => query.GetExecutableCriteria(session).List<TR>().FirstOrDefault());
		}

		public static IEnumerable<T> SlicedFindAll<T>(this DetachedCriteria query, int firstResult, int maxResults) where T : class
		{
			return AR.Execute<T, IEnumerable<T>>(session => query.GetExecutableCriteria(session)
				.SetFirstResult(firstResult)
				.SetMaxResults(maxResults)
				.List<T>());
		}

		public static void DeleteAll<T>(this DetachedCriteria query) where T : class
		{
			AR.DeleteAll<T>(query);
		}

		public static IEnumerable<T> List<T>(this IDetachedQuery query) where T : class
		{
			return AR.Execute<T, IEnumerable<T>>(session => query.GetExecutableQuery(session).List<T>());
		}

		public static IEnumerable<T> SlicedFindAll<T>(this IDetachedQuery query, int firstResult, int maxResults) where T : class
		{
			return AR.Execute<T, IEnumerable<T>>(session => query.GetExecutableQuery(session)
					.SetFirstResult(firstResult)
					.SetMaxResults(maxResults)
					.List<T>());
		}

		public static IEnumerable<TR> List<T, TR>(this IDetachedQuery query) where T : class
		{
			return AR.Execute<T, IEnumerable<TR>>(session => query.GetExecutableQuery(session).List<TR>());
		}

		public static DetachedCriteria AddOrders(this DetachedCriteria criteria, params Order[] orders) {
			orders.ForEach(o => criteria.AddOrder(o));
			return criteria;
		}

		public static DetachedCriteria AddCriterias(this DetachedCriteria criteria, params ICriterion[] criterias) {
			criterias.ForEach(o => criteria.Add(o));
			return criteria;
		}
	}
}
