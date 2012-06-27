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
using System.Linq.Expressions;
using Castle.Core.Internal;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Impl;

namespace Castle.ActiveRecord {
	public static class QueryExtensions {

		#region QueryOver
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

		public static int RowCount<T>(this QueryOver<T> query) where T : class
		{
			return AR.Execute<T, int>(session => query.GetExecutableQueryOver(session).RowCount());
		}

		public static IFutureValue<TR> FutureValue<T, TR>(this QueryOver<T> query) where T : class
		{
			return AR.Execute<T, IFutureValue<TR>>(session => query.GetExecutableQueryOver(session).FutureValue<TR>());
		}

		public static IEnumerable<T> SlicedFindAll<T>(this QueryOver<T> query, int firstResult, int maxResults) where T : class
		{
			return AR.Execute<T, IEnumerable<T>>(session => query.GetExecutableQueryOver(session)
				.Skip(firstResult)
				.Take(maxResults)
				.List<T>());
		}

		public static bool Exists<T>(this QueryOver<T, T> queryover) where T : class
		{
			return queryover.Count<T>() > 0;
		}

		public static int Count<T>(this QueryOver<T, T> queryover) where T : class
		{
			return queryover.ToRowCountQuery().RowCount<T>();
		}

		public static void DeleteAll<T>(this QueryOver<T, T> query) where T : class
		{
			AR.DeleteAll<T>(query);
		}

		public static T FindOne<T>(this QueryOver<T,T> queryover) where T : class
		{
			var result = queryover.SlicedFindAll<T>(0, 2).ToList();

			if (result.Count > 1)
			{
				throw new ActiveRecordException("ActiveRecord.FindOne returned " + result.Count() +
												" rows. Expecting one or none");
			}

			return result.FirstOrDefault();
		}
		#endregion

		#region DetachedCriteria
		public static IEnumerable<T> List<T>(this DetachedCriteria query) where T : class
		{
			return AR.Execute<T, IEnumerable<T>>(session => query.GetExecutableCriteria(session).List<T>());
		}

		public static IEnumerable<TR> List<T, TR>(this DetachedCriteria query) where T : class
		{
			return AR.Execute<T, IEnumerable<TR>>(session => query.GetExecutableCriteria(session).List<TR>());
		}

		public static IEnumerable<T> Future<T>(this DetachedCriteria query) where T : class
		{
			return AR.Execute<T, IEnumerable<T>>(session => query.GetExecutableCriteria(session).Future<T>());
		}

		public static IEnumerable<TR> Future<T, TR>(this DetachedCriteria query) where T : class
		{
			return AR.Execute<T, IEnumerable<TR>>(session => query.GetExecutableCriteria(session).Future<TR>());
		}

		public static TR UniqueResult<T, TR>(this DetachedCriteria query) where T : class
		{
			return AR.Execute<T, TR>(session => query.GetExecutableCriteria(session).UniqueResult<TR>());
		}

		public static IFutureValue<TR> FutureValue<T, TR>(this DetachedCriteria query) where T : class
		{
			return AR.Execute<T, IFutureValue<TR>>(session => query.GetExecutableCriteria(session).FutureValue<TR>());
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

		public static DetachedCriteria AddOrders(this DetachedCriteria criteria, params Order[] orders) {
			orders.ForEach(o => criteria.AddOrder(o));
			return criteria;
		}

		public static DetachedCriteria AddCriterias(this DetachedCriteria criteria, params ICriterion[] criterias) {
			criterias.ForEach(o => criteria.Add(o));
			return criteria;
		}

		public static DetachedCriteria AddOrder<T>(this DetachedCriteria criteria, Expression<Func<T, object>> expression) {
			return criteria.AddOrder(expression, true);
		}

		public static DetachedCriteria AddOrder<T>(this DetachedCriteria criteria, Expression<Func<T, object>> expression, bool asc) {
			return
				criteria.AddOrder(asc
									? Order.Asc(Projections.Property(expression))
									: Order.Desc(Projections.Property(expression)));
		}

		public static ICriteria AddOrder<T>(this ICriteria criteria, Expression<Func<T, object>> expression) {
			return criteria.AddOrder(expression, true);
		}

		public static ICriteria AddOrder<T>(this ICriteria criteria, Expression<Func<T, object>> expression, bool asc) {
			return criteria.AddOrder(asc ?
										Order.Asc(Projections.Property(expression))
										: Order.Desc(Projections.Property(expression)));
		}
		#endregion

		#region DetachedQuery
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
		#endregion
	}
}
