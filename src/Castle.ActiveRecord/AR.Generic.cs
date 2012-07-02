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


using System.Linq.Expressions;

namespace Castle.ActiveRecord
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using NHibernate;

	using NHibernate.Linq;
	using NHibernate.Transform;
	using NHibernate.Criterion;

	/// <summary>
	/// Allow programmers to use the 
	/// ActiveRecord functionality without extending ActiveRecordBase/>
	/// </summary>
	public static partial class AR {

		#region Execute/ExecuteStateless

		/// <summary>
		/// Invokes the specified delegate passing a valid 
		/// NHibernate session. Used for custom NHibernate queries.
		/// </summary>
		/// <param name="func">The delegate instance</param>
		/// <param name="instance">The ActiveRecord instance</param>
		/// <returns>Whatever is returned by the delegate invocation</returns>
		public static TK Execute<T, TK>(Func<ISession, T, TK> func, T instance) where T : class {
			if (func == null) throw new ArgumentNullException("func", "Delegate must be passed");

			EnsureInitialized(typeof(T));

			var session = Holder.CreateSession(typeof (T));

			try {
				return func(session, instance);

			} catch (ObjectNotFoundException ex) {
				var message = string.Format("Could not find {0} with id {1}", ex.EntityName, ex.Identifier);
				throw new NotFoundException(message, ex);

			} catch (Exception ex) {
				Holder.FailSession(session);
				throw new ActiveRecordException("Error performing Execute for " + typeof (T).Name, ex);

			} finally {
				Holder.ReleaseSession(session);
			}
		}

		/// <summary>
		/// Invokes the specified delegate passing a valid 
		/// NHibernate session. Used for custom NHibernate queries.
		/// </summary>
		/// <param name="action">The delegate instance</param>
		public static void Execute<T>(Action<ISession> action) where T : class {
			Execute<T, string>(session => {
				action(session);
				return string.Empty;
			});
		}

		/// <summary>
		/// Invokes the specified delegate passing a valid 
		/// NHibernate session. Used for custom NHibernate queries.
		/// </summary>
		/// <param name="func">The delegate instance</param>
		/// <returns>Whatever is returned by the delegate invocation</returns>
		public static TK Execute<T, TK>(Func<ISession, TK> func) where T : class {
			return Execute<T, TK>((session, arg2) => func(session), null);
		}

		/// <summary>
		/// Invokes the specified delegate passing a valid 
		/// NHibernate stateless session. Used for custom NHibernate queries.
		/// </summary>
		/// <param name="func">The delegate instance</param>
		/// <param name="instance">The ActiveRecord instance</param>
		/// <returns>Whatever is returned by the delegate invocation</returns>
		public static TK ExecuteStateless<T, TK>(Func<IStatelessSession, T, TK> func, T instance) where T : class {
			if (func == null) throw new ArgumentNullException("func", "Delegate must be passed");

			EnsureInitialized(typeof(T));

			var session = Holder.GetSessionFactory(typeof (T)).OpenStatelessSession();
			var tx = session.BeginTransaction();
			try {
				var result = func(session, instance);
				tx.Commit();
				return result;
			} catch (Exception ex) {
				tx.Rollback();
				throw new ActiveRecordException("Error performing Execute for " + typeof (T).Name, ex);

			} finally {
				tx.Dispose();
				session.Dispose();
			}
		}

		/// <summary>
		/// Invokes the specified delegate passing a valid 
		/// NHibernate stateless session. Used for custom NHibernate queries.
		/// </summary>
		/// <param name="func">The delegate instance</param>
		/// <returns>Whatever is returned by the delegate invocation</returns>
		public static TK ExecuteStateless<T, TK>(Func<IStatelessSession, TK> func) where T : class {
			return ExecuteStateless<T, TK>((session, arg2) => func(session), null);
		}

		/// <summary>
		/// Invokes the specified delegate passing a valid 
		/// NHibernate stateless session. Used for custom NHibernate queries.
		/// </summary>
		/// <param name="action">The delegate instance</param>
		public static void ExecuteStateless<T>(Action<IStatelessSession> action) where T : class {
			ExecuteStateless<T, object>(session => {
				action(session);
				return string.Empty;
			});
		}

		#endregion

		#region Find/Peek

		/// <summary>
		/// Finds an object instance by its primary key
		/// returns null if not found
		/// </summary>
		/// <param name="id">Identifier value</param>
		public static T Find<T>(object id) where T : class {
			return id == null ? null : Execute<T, T>(session => session.Get<T>(ConvertId<T>(id)));
		}

		/// <summary>
		/// Peeks for an object instance by its primary key,
		/// never returns null
		/// </summary>
		/// <param name="id">Identifier value</param>
		public static T Peek<T>(object id) where T : class
		{
			return Execute<T, T>(session => session.Load<T>(ConvertId<T>(id)));
		}

		#endregion

		#region Exists/Count

		/// <summary>
		/// Check if any instance matches the query.
		/// </summary>
		/// <param name="detachedQuery">The query expression</param>
		/// <returns><c>true</c> if an instance is found; otherwise <c>false</c>.</returns>
		public static bool Exists<T>(IDetachedQuery detachedQuery) where T : class
		{
			return SlicedFindAll<T>(0, 1, detachedQuery).Any();
		}

		/// <summary>
		/// Check if any instance matches the criteria.
		/// </summary>
		/// <returns><c>true</c> if an instance is found; otherwise <c>false</c>.</returns>
		public static bool Exists<T>(params ICriterion[] criterias) where T : class
		{
			return Count<T>(criterias) > 0;
		}

		/// <summary>
		/// Check if any instance matching the criteria exists in the database.
		/// </summary>
		/// <param name="expression">The queryover expression</param>
		/// <returns><c>true</c> if an instance is found; otherwise <c>false</c>.</returns>
		public static bool Exists<T>(Expression<Func<T, bool>> expression) where T : class
		{
			return Count<T>(expression) > 0;
		}

		/// <summary>
		/// Check if any instance matching the criteria exists in the database.
		/// </summary>
		/// <param name="queryover">The queryover expression</param>
		/// <returns><c>true</c> if an instance is found; otherwise <c>false</c>.</returns>
		public static bool Exists<T>(QueryOver<T, T> queryover) where T : class
		{
			return Count<T>(queryover) > 0;
		}

		/// <summary>
		/// Check if any instance matching the criteria exists in the database.
		/// </summary>
		/// <param name="detachedCriteria">The criteria expression</param>
		/// <returns><c>true</c> if an instance is found; otherwise <c>false</c>.</returns>
		public static bool Exists<T>(DetachedCriteria detachedCriteria) where T : class
		{
			return Count<T>(detachedCriteria) > 0;
		}

		/// <summary>
		/// Returns the number of records of the specified 
		/// type in the database that match the given critera
		/// </summary>
		/// <param name="criteria">The criteria expression</param>
		/// <returns>The count result</returns>
		public static int Count<T>(params ICriterion[] criteria) where T : class
		{
			var dc = DetachedCriteria.For<T>()
				.AddCriterias(criteria);

			return Count<T>(dc);
		}

		/// <summary>
		/// Returns the number of records of the specified 
		/// type in the database
		/// </summary>
		/// <param name="expression">The criteria expression</param>
		/// <returns>The count result</returns>
		public static int Count<T>(Expression<Func<T, bool>> expression) where T : class
		{
			return NHibernate.Criterion.QueryOver.Of<T>().Where(expression).Count();
		}

		/// <summary>
		/// Returns the number of records of the specified 
		/// type in the database
		/// </summary>
		/// <param name="queryover">The criteria expression</param>
		/// <returns>The count result</returns>
		public static int Count<T>(QueryOver<T, T> queryover) where T : class
		{
			return queryover.Count();
		}

		/// <summary>
		/// Returns the number of records of the specified 
		/// type in the database
		/// </summary>
		/// <param name="detachedCriteria">The criteria expression</param>
		/// <returns>The count result</returns>
		public static int Count<T>(DetachedCriteria detachedCriteria) where T : class
		{
			return detachedCriteria.SetProjection(Projections.RowCount()).UniqueResult<T, int>();
		}

		#endregion

		#region FindFirst

		/// <summary>
		/// Searches and returns the first row for <typeparamref name="T"/>
		/// </summary>
		/// <param name="order">The sort order - used to determine which record is the first one</param>
		/// <param name="criteria">The criteria expression</param>
		/// <returns>A <c>targetType</c> instance or <c>null</c></returns>
		public static T FindFirst<T>(Order order, params ICriterion[] criteria) where T : class
		{
			return FindFirst<T>(new[] {order}, criteria);
		}

		/// <summary>
		/// Searches and returns the first row.
		/// </summary>
		/// <param name="orders">The sort order - used to determine which record is the first one</param>
		/// <param name="criterias">The criteria expression</param>
		/// <returns>A <c>targetType</c> instance or <c>null</c></returns>
		public static T FindFirst<T>(Order[] orders, params ICriterion[] criterias) where T : class
		{
			return SlicedFindAll<T>(0, 1, orders, criterias).FirstOrDefault();
		}

		/// <summary>
		/// Searches and returns the first row.
		/// </summary>
		/// <param name="criterias">The criteria expression</param>
		/// <returns>A <c>targetType</c> instance or <c>null</c></returns>
		public static T FindFirst<T>(params ICriterion[] criterias) where T : class
		{
			return SlicedFindAll<T>(0, 1, criterias).FirstOrDefault();
		}

		/// <summary>
		/// Searches and returns the first row.
		/// </summary>
		/// <param name="detachedCriteria">The criteria.</param>
		/// <param name="orders">The sort order - used to determine which record is the first one.</param>
		/// <returns>A <c>targetType</c> instance or <c>null.</c></returns>
		public static T FindFirst<T>(DetachedCriteria detachedCriteria, params Order[] orders) where T : class
		{
			return SlicedFindAll<T>(0, 1, detachedCriteria, orders).FirstOrDefault();
		}

		/// <summary>
		/// Searches and returns the first row.
		/// </summary>
		/// <param name="detachedQuery">The expression query.</param>
		/// <returns>A <c>targetType</c> instance or <c>null.</c></returns>
		public static T FindFirst<T>(IDetachedQuery detachedQuery) where T : class
		{
			return SlicedFindAll<T>(0, 1, detachedQuery).FirstOrDefault();
		}

		#endregion

		#region FindOne

		/// <summary>
		/// Searches and returns the first row.
		/// </summary>
		/// <param name="criterias">The criterias.</param>
		/// <returns>A instance the targetType or <c>null</c></returns>
		public static T FindOne<T>(params ICriterion[] criterias) where T : class
		{
			var result = SlicedFindAll<T>(0, 2, criterias).ToList();

			if (result.Count > 1)
			{
				throw new ActiveRecordException("ActiveRecord.FindOne returned " + result.Count() +
												" rows. Expecting one or none");
			}

			return result.FirstOrDefault();
		}

		/// <summary>
		/// Searches and returns a row. If more than one is found, 
		/// throws <see cref="ActiveRecordException"/>
		/// </summary>
		/// <param name="queryover">The QueryOver</param>
		/// <returns>A <c>targetType</c> instance or <c>null</c></returns>
		public static T FindOne<T>(QueryOver<T,T> queryover) where T : class
		{
			return queryover.FindOne();
		}

		/// <summary>
		/// Searches and returns a row. If more than one is found, 
		/// throws <see cref="ActiveRecordException"/>
		/// </summary>
		/// <param name="criteria">The criteria</param>
		/// <returns>A <c>targetType</c> instance or <c>null</c></returns>
		public static T FindOne<T>(DetachedCriteria criteria) where T : class
		{
			var result = SlicedFindAll<T>(0, 2, criteria).ToList();

			if (result.Count > 1)
			{
				throw new ActiveRecordException("ActiveRecord.FindOne returned " + result.Count() +
												" rows. Expecting one or none");
			}

			return result.FirstOrDefault();
		}

		/// <summary>
		/// Searches and returns a row. If more than one is found,
		/// throws <see cref="ActiveRecordException"/>
		/// </summary>
		/// <param name="detachedQuery">The query expression</param>
		/// <returns>A <c>targetType</c> instance or <c>null</c></returns>
		public static T FindOne<T>(IDetachedQuery detachedQuery) where T : class
		{
			var result = SlicedFindAll<T>(0, 2, detachedQuery).ToList();

			if (result.Count > 1)
			{
				throw new ActiveRecordException("ActiveRecord.FindOne returned " + result.Count() +
												" rows. Expecting one or none");
			}

			return result.FirstOrDefault();
		}

		#endregion

		#region FindAllByProperty
		/// <summary>
		/// Finds records based on a property value - automatically converts null values to IS NULL style queries. 
		/// </summary>
		/// <param name="property">A property name (not a column name)</param>
		/// <param name="value">The value to be equals to</param>
		/// <returns></returns>
		public static IEnumerable<T> FindAllByProperty<T>(string property, object value) where T : class
		{
			ICriterion criteria = (value == null) ? Restrictions.IsNull(property) : Restrictions.Eq(property, value);
			return FindAll<T>(criteria);
		}

		/// <summary>
		/// Finds records based on a property value - automatically converts null values to IS NULL style queries. 
		/// </summary>
		/// <param name="orderByColumn">The column name to be ordered ASC</param>
		/// <param name="property">A property name (not a column name)</param>
		/// <param name="value">The value to be equals to</param>
		/// <returns></returns>
		public static IEnumerable<T> FindAllByProperty<T>(string orderByColumn, string property, object value) where T : class
		{
			return DetachedCriteria.For<T>()
				.Add(
					(value == null) ? Restrictions.IsNull(property) : Restrictions.Eq(property, value)
				).AddOrder(Order.Asc(orderByColumn))
				.List<T>();
		}

		#endregion

		#region FindAll

		/// <summary>
		/// Returns all instances found for the specified type 
		/// using sort orders and criteria.
		/// </summary>
		/// <param name="order">An <see cref="Order"/> object.</param>
		/// <param name="criteria">The criteria expression</param>
		/// <returns>The <see cref="Array"/> of results.</returns>
		public static IEnumerable<T> FindAll<T>(Order order, params ICriterion[] criteria) where T : class
		{
			return FindAll<T>(new[] {order}, criteria);
		}

		/// <summary>
		/// Returns all instances found for the specified type 
		/// using sort orders and criterias.
		/// </summary>
		/// <param name="orders"></param>
		/// <param name="criterias"></param>
		/// <returns></returns>
		public static IEnumerable<T> FindAll<T>(Order[] orders, params ICriterion[] criterias) where T : class 
		{
			return DetachedCriteria.For<T>()
				.SetResultTransformer(Transformers.DistinctRootEntity)
				.AddCriterias(criterias)
				.AddOrders(orders)
				.List<T>();
		}

		/// <summary>
		/// Returns all instances found for the specified type 
		/// using criterias.
		/// </summary>
		/// <param name="criterias"></param>
		/// <returns></returns>
		public static IEnumerable<T> FindAll<T>(params ICriterion[] criterias) where T : class
		{
			return DetachedCriteria.For<T>()
				.SetResultTransformer(Transformers.DistinctRootEntity)
				.AddCriterias(criterias)
				.List<T>();
		}

		/// <summary>
		/// Returns all instances found for the specified type according to the criteria
		/// </summary>
		public static IEnumerable<T> FindAll<T>(QueryOver<T, T> queryover) where T : class
		{
			return queryover.List();
		}

		/// <summary>
		/// Returns all instances found for the specified type according to the criteria
		/// </summary>
		public static IEnumerable<T> FindAll<T>(DetachedCriteria detachedCriteria, params Order[] orders) where T : class
		{
			return detachedCriteria.AddOrders(orders).List<T>();
		}

		/// <summary>
		/// Returns all instances found for the specified type according to the criteria
		/// </summary>
		/// <param name="detachedQuery">The query expression</param>
		/// <returns>The <see cref="Array"/> of results.</returns>
		public static IEnumerable<T> FindAll<T>(IDetachedQuery detachedQuery) where T : class
		{
			return detachedQuery.List<T>();
		}

		#endregion

		#region SlicedFindAll

		/// <summary>
		/// Returns a portion of the query results (sliced)
		/// </summary>
		/// <param name="firstResult">The number of the first row to retrieve.</param>
		/// <param name="maxResults">The maximum number of results retrieved.</param>
		/// <param name="order">order</param>
		/// <param name="criteria">criteria</param>
		/// <returns>The sliced query results.</returns>
		public static IEnumerable<T> SlicedFindAll<T>(int firstResult, int maxResults, Order order, params ICriterion[] criteria) where T : class
		{
			return SlicedFindAll<T>(firstResult, maxResults, DetachedCriteria.For<T>().AddCriterias(criteria), order);
		}

		/// <summary>
		/// Returns a portion of the query results (sliced)
		/// </summary>
		/// <param name="firstResult">The number of the first row to retrieve.</param>
		/// <param name="maxResults">The maximum number of results retrieved.</param>
		/// <param name="orders">An <see cref="Array"/> of <see cref="Order"/> objects.</param>
		/// <param name="criteria">The criteria expression</param>
		/// <returns>The sliced query results.</returns>
		public static IEnumerable<T> SlicedFindAll<T>(int firstResult, int maxResults, Order[] orders, params ICriterion[] criteria) where T : class
		{
			return SlicedFindAll<T>(firstResult, maxResults, DetachedCriteria.For<T>().AddCriterias(criteria), orders);
		}

		/// <summary>
		/// Returns a portion of the query results (sliced)
		/// </summary>
		/// <param name="firstResult">The number of the first row to retrieve.</param>
		/// <param name="maxResults">The maximum number of results retrieved.</param>
		/// <param name="criteria">The criteria expression</param>
		/// <returns>The sliced query results.</returns>
		public static IEnumerable<T> SlicedFindAll<T>(int firstResult, int maxResults, params ICriterion[] criteria) where T : class
		{
			return SlicedFindAll<T>(firstResult, maxResults, DetachedCriteria.For<T>().AddCriterias(criteria));
		}

		/// <summary>
		/// Returns a portion of the query results (sliced)
		/// </summary>
		/// <param name="firstResult">The number of the first row to retrieve.</param>
		/// <param name="maxResults">The maximum number of results retrieved.</param>
		/// <param name="orders">An <see cref="Array"/> of <see cref="Order"/> objects.</param>
		/// <param name="criteria">The criteria expression</param>
		/// <returns>The sliced query results.</returns>
		public static IEnumerable<T> SlicedFindAll<T>(int firstResult, int maxResults, DetachedCriteria criteria, params Order[] orders) where T : class
		{
			return criteria
				.AddOrders(orders)
				.SlicedFindAll<T>(firstResult, maxResults);
		}

		/// <summary>
		/// Returns a portion of the query results (sliced)
		/// </summary>
		/// <param name="firstResult">The number of the first row to retrieve.</param>
		/// <param name="maxResults">The maximum number of results retrieved.</param>
		/// <param name="detachedQuery">The query expression</param>
		/// <returns>The sliced query results.</returns>
		public static IEnumerable<T> SlicedFindAll<T>(int firstResult, int maxResults, IDetachedQuery detachedQuery) where T : class
		{
			return detachedQuery.SlicedFindAll<T>(firstResult, maxResults);
		}

		/// <summary>
		/// Returns a portion of the query results (sliced)
		/// </summary>
		/// <param name="firstResult">The number of the first row to retrieve.</param>
		/// <param name="maxResults">The maximum number of results retrieved.</param>
		/// <param name="queryover">Queryover</param>
		/// <returns>The sliced query results.</returns>
		public static IEnumerable<T> SlicedFindAll<T>(int firstResult, int maxResults, QueryOver<T, T> queryover) where T : class
		{
			return queryover.SlicedFindAll<T>(firstResult, maxResults);
		}

		#endregion

		#region DeleteAll

		/// <summary>
		/// Deletes all rows for the specified ActiveRecord type that matches
		/// the supplied criteria
		/// </summary>
		public static void DeleteAll<T>(DetachedCriteria criteria) where T : class
		{
			var pks = criteria.SetProjection(Projections.Id()).List<T, object>();
			DeleteAll<T>(pks);
		}

		/// <summary>
		/// Deletes all rows for the specified ActiveRecord type that matches
		/// the supplied criteria
		/// </summary>
		public static void DeleteAll<T>(params ICriterion[] criteria) where T : class
		{
			if (criteria != null && criteria.Length > 0)
				DeleteAll<T>(DetachedCriteria.For<T>().AddCriterias(criteria));
			else
				Execute<T>(session => {
					session.CreateQuery("delete from " + typeof(T).FullName)
						.ExecuteUpdate();
					session.Flush();
				});

		}

		/// <summary>
		/// Deletes all rows for the specified ActiveRecord type that matches
		/// the supplied expression criteria
		/// </summary>
		public static void DeleteAll<T>(Expression<Func<T, bool>> expression) where T : class
		{
			var pks = NHibernate.Criterion.QueryOver.Of<T>().Where(expression).Select(Projections.Id()).List<T, object>();
			DeleteAll<T>(pks);
		}

		/// <summary>
		/// Deletes all rows for the specified ActiveRecord type that matches
		/// the supplied queryover
		/// </summary>
		public static void DeleteAll<T>(QueryOver<T, T> queryover) where T : class
		{
			var pks = queryover.Select(Projections.Id()).List<T, object>();
			DeleteAll<T>(pks);
		}

		/// <summary>
		/// Deletes all rows for the specified ActiveRecord type that matches
		/// the supplied HQL condition
		/// </summary>
		/// <param name="where">HQL condition to select the rows to be deleted</param>
		public static void DeleteAll<T>(string where) where T : class
		{
			Execute<T>(session => {
				session.Delete(string.Format("from {0} where {1}", typeof(T).FullName, where));
				session.Flush();
			});
		}

		/// <summary>
		/// Deletes all rows for the supplied primary keys 
		/// </summary>
		/// <param name="pkvalues">A list of primary keys</param>
		public static void DeleteAll<T>(IEnumerable<object> pkvalues) where T : class
		{
			var cm = Holder.GetSessionFactory(typeof (T)).GetClassMetadata(typeof (T));
			var pkname = cm.IdentifierPropertyName;
			var pktype = cm.IdentifierType.ReturnedClass;

			if (pktype == typeof(int) || pktype == typeof(long))
			{
				const string hql = "delete from {0} _this where _this.{1} in ({2})";
				Execute<T>(session =>
					session.CreateQuery(string.Format(hql, typeof(T).FullName, pkname, string.Join(",", pkvalues)))
						.ExecuteUpdate()
				);
			}
			else if (pktype == typeof(Guid) || pktype == typeof(string))
			{
				const string hql = "delete from {0} _this where _this.{1} in ('{2}')";
				Execute<T>(session =>
					session.CreateQuery(string.Format(hql, typeof(T).FullName, pkname, string.Join("','", pkvalues)))
						.ExecuteUpdate()
				);
				
			}
			else
			{
				Execute<T>(session => {
					foreach (var obj in pkvalues.Select(id => Peek<T>(id)).Where(o => o != null)) {
						Delete(obj);
					}
				});
			}
		}

		#endregion

		#region Save/Flush

		/// <summary>
		/// Saves the instance to the database. If the primary key is unitialized
		/// it creates the instance on the database. Otherwise it updates it.
		/// <para>
		/// If the primary key is assigned, then you must invoke <see cref="Create{T}"/>
		/// or <see cref="Update{T}"/> instead.
		/// </para>
		/// </summary>
		/// <param name="instance">The ActiveRecord instance to be saved</param>
		public static void Save<T>(T instance) where T : class
		{
			InternalSave(instance, false);
		}

		/// <summary>
		/// Saves the instance to the database and flushes the session. If the primary key is unitialized
		/// it creates the instance on the database. Otherwise it updates it.
		/// <para>
		/// If the primary key is assigned, then you must invoke <see cref="Create{T}"/>
		/// or <see cref="Update{T}"/> instead.
		/// </para>
		/// </summary>
		/// <param name="instance">The ActiveRecord instance to be saved</param>
		public static void SaveAndFlush<T>(T instance) where T : class
		{
			InternalSave(instance, true);
		}

		/// <summary>
		/// Saves the instance to the database. If the primary key is unitialized
		/// it creates the instance on the database. Otherwise it updates it.
		/// <para>
		/// If the primary key is assigned, then you must invoke <see cref="Create{T}"/>
		/// or <see cref="Update{T}"/> instead.
		/// </para>
		/// </summary>
		/// <param name="instance">The ActiveRecord instance to be saved</param>
		/// <param name="flush">if set to <c>true</c>, the operation will be followed by a session flush.</param>
		private static void InternalSave<T>(T instance, bool flush) where T : class
		{
			if (instance == null) throw new ArgumentNullException("instance");
			Execute<T>(session => {
				session.SaveOrUpdate(instance);
				if (flush) session.Flush();
			});
		}

		#endregion

		#region SaveCopy/Flush

		/// <summary>
		/// Saves a copy of the instance to the database. If the primary key is uninitialized
		/// it creates the instance in the database. Otherwise it updates it.
		/// <para>
		/// If the primary key is assigned, then you must invoke <see cref="Create{T}"/>
		/// or <see cref="Update{T}"/> instead.
		/// </para>
		/// </summary>
		/// <param name="instance">The transient instance to be saved</param>
		/// <returns>The saved ActiveRecord instance</returns>
		public static T SaveCopy<T>(T instance) where T : class
		{
			return InternalSaveCopy(instance, false);
		}

		/// <summary>
		/// Saves a copy of the instance to the database and flushes the session. If the primary key is uninitialized
		/// it creates the instance in the database. Otherwise it updates it.
		/// <para>
		/// If the primary key is assigned, then you must invoke <see cref="Create{T}"/>
		/// or <see cref="Update{T}"/> instead.
		/// </para>
		/// </summary>
		/// <param name="instance">The transient instance to be saved</param>
		/// <returns>The saved ActiveRecord instance</returns>
		public static T SaveCopyAndFlush<T>(T instance) where T : class
		{
			return InternalSaveCopy(instance, true);
		}

		/// <summary>
		/// Saves a copy of the instance to the database. If the primary key is unitialized
		/// it creates the instance on the database. Otherwise it updates it.
		/// <para>
		/// If the primary key is assigned, then you must invoke <see cref="Create{T}"/>
		/// or <see cref="Update{T}"/> instead.
		/// </para>
		/// </summary>
		/// <param name="instance">The transient instance to be saved</param>
		/// <param name="flush">if set to <c>true</c>, the operation will be followed by a session flush.</param>
		/// <returns>The saved ActiveRecord instance.</returns>
		private static T InternalSaveCopy<T>(T instance, bool flush) where T : class
		{
			if (instance == null) throw new ArgumentNullException("instance");
			return Execute<T, T>(session => {
				var persistent = session.Merge(instance);
				if (flush) session.Flush();
				return persistent;
			});
		}

		#endregion

		#region Create/Flush

		/// <summary>
		/// Creates (Saves) a new instance to the database.
		/// </summary>
		/// <param name="instance"></param>
		public static void Create<T>(T instance) where T : class
		{
			InternalCreate(instance, false);
		}

		/// <summary>
		/// Creates (Saves) a new instance to the database.
		/// </summary>
		/// <param name="instance"></param>
		public static void CreateAndFlush<T>(T instance) where T : class
		{
			InternalCreate(instance, false);
		}

		/// <summary>
		/// Creates (Saves) a new instance to the database.
		/// </summary>
		/// <param name="instance">The ActiveRecord instance to be updated on the database</param>
		/// <param name="flush">if set to <c>true</c>, the operation will be followed by a session flush.</param>
		private static void InternalCreate<T>(T instance, bool flush) where T : class
		{
			if (instance == null) throw new ArgumentNullException("instance");
			Execute<T>(session => {
				session.Save(instance);
				if (flush) session.Flush();
			});
		}

		#endregion

		#region Update/Flush

		/// <summary>
		/// Persists the modification on the instance
		/// state to the database.
		/// </summary>
		/// <param name="instance"></param>
		public static void Update<T>(T instance) where T : class
		{
			InternalUpdate(instance, false);
		}

		/// <summary>
		/// Persists the modification on the instance
		/// state to the database and flushes the session.
		/// </summary>
		/// <param name="instance">The ActiveRecord instance to be updated on the database</param>
		public static void UpdateAndFlush<T>(T instance) where T : class
		{
			InternalUpdate(instance, true);
		}

		/// <summary>
		/// Persists the modification on the instance
		/// state to the database.
		/// </summary>
		/// <param name="instance">The ActiveRecord instance to be updated on the database</param>
		/// <param name="flush">if set to <c>true</c>, the operation will be followed by a session flush.</param>
		private static void InternalUpdate<T>(T instance, bool flush) where T : class
		{
			if (instance == null) throw new ArgumentNullException("instance");
			Execute<T>(session => {
				session.Update(instance);

				if (flush) session.Flush();
			});
		}

		#endregion

		#region Delete/Flush

		/// <summary>
		/// Deletes the instance from the database.
		/// </summary>
		/// <param name="instance">The ActiveRecord instance to be deleted</param>
		public static void Delete<T>(T instance) where T : class 
		{
			InternalDelete(instance, false);
		}

		/// <summary>
		/// Deletes the instance from the database and flushes the session.
		/// </summary>
		/// <param name="instance">The ActiveRecord instance to be deleted</param>
		public static void DeleteAndFlush<T>(T instance) where T : class
		{
			InternalDelete(instance, true);
		}

		/// <summary>
		/// Deletes the instance from the database.
		/// </summary>
		/// <param name="instance">The ActiveRecord instance to be deleted</param>
		/// <param name="flush">if set to <c>true</c>, the operation will be followed by a session flush.</param>
		private static void InternalDelete<T>(T instance, bool flush) where T : class
		{
			if (instance == null) throw new ArgumentNullException("instance");
			Execute<T>(session => {
				session.Delete(instance);
				if (flush) session.Flush();
			});
		}

		#endregion

		#region Refresh/Merge/Evict/Replicate

		/// <summary>
		/// Refresh the instance from the database.
		/// </summary>
		/// <param name="instance">The ActiveRecord instance to be reloaded</param>
		public static void Refresh<T>(T instance) where T : class
		{
			if (instance == null) throw new ArgumentNullException("instance");
			Execute<T>(session => session.Refresh(instance));
		}

		/// <summary>
		/// Merge the instance to scope session
		/// </summary>
		/// <param name="instance"></param>
		public static void Merge<T>(T instance) where T : class
		{
			if (instance == null) throw new ArgumentNullException("instance");
			Execute<T>(session => session.Merge(instance));
		}

		/// <summary>
		/// Evict the instance from scope session
		/// </summary>
		/// <param name="instance"></param>
		public static void Evict<T>(object instance) where T : class
		{
			if (instance == null) throw new ArgumentNullException("instance");
			Execute<T>(session => session.Evict(instance));
		}

		/// <summary>
		/// From NHibernate documentation: 
		/// Persist all reachable transient objects, reusing the current identifier 
		/// values. Note that this will not trigger the Interceptor of the Session.
		/// </summary>
		/// <param name="instance">The instance.</param>
		/// <param name="replicationMode">The replication mode.</param>
		public static void Replicate<T>(object instance, ReplicationMode replicationMode) where T : class
		{
			if (instance == null) { throw new ArgumentNullException("instance"); }
			Execute<T>(session => session.Replicate(instance, replicationMode));
		}

		#endregion

		#region Linq/QueryOver

		/// <summary>
		/// Provide an IQueryable.
		/// Make sure we are in a scope
		/// </summary>
		public static IQueryable<T> All<T>() where T : class
		{
			if (!Holder.ThreadScopeInfo.HasInitializedScope)
				throw new ActiveRecordException("You need to be in an ISessionScope to do linq queries.");

			return Execute<T, IQueryable<T>>(s => s.Query<T>());
		}


		/// <summary>
		/// The QueryOver method is used as a Linq collection
		/// or as the in argument in a Linq expression. 
		/// </summary>
		/// <remarks>You must have an open Session Scope.</remarks>
		public static QueryOver<T,T> QueryOver<T>() where T : class
		{
			return NHibernate.Criterion.QueryOver.Of<T>();
		}

		/// <summary>
		/// The QueryOver method is used as a Linq collection
		/// or as the in argument in a Linq expression. 
		/// </summary>
		/// <remarks>You must have an open Session Scope.</remarks>
		public static QueryOver<T,T> QueryOver<T>(Expression<Func<T>> alias) where T : class
		{
			return NHibernate.Criterion.QueryOver.Of(alias);
		}

		/// <summary>
		/// The QueryOver method is used as a Linq collection
		/// or as the in argument in a Linq expression. 
		/// </summary>
		/// <remarks>You must have an open Session Scope.</remarks>
		public static QueryOver<T,T> QueryOver<T>(string entityname) where T : class
		{
			return NHibernate.Criterion.QueryOver.Of<T>(entityname);
		}

		/// <summary>
		/// The QueryOver method is used as a Linq collection
		/// or as the in argument in a Linq expression. 
		/// </summary>
		/// <remarks>You must have an open Session Scope.</remarks>
		public static QueryOver<T,T> QueryOver<T>(string entityname, Expression<Func<T>> alias) where T : class
		{
			return NHibernate.Criterion.QueryOver.Of(entityname, alias);
		}

		#endregion

		internal static void EnsureInitialized(Type type)
		{
			if (!IsInitialized)
			{
				var message = string.Format("An ActiveRecord class ({0}) was used but the framework seems not " +
											   "properly initialized. Did you forget about ActiveRecordStarter.Initialize() ?",
											   type.FullName);
				throw new ActiveRecordException(message);
			}

			if (!Holder.IsInitialized(type))
			{
				throw new ActiveRecordException("No configuration for ActiveRecord found in the type hierarchy -> " + type.FullName);
			}
		}
	}
}
