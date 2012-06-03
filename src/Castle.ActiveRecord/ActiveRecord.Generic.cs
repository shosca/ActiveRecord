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

	using Castle.Components.Validator;
	using NHibernate;

	using NHibernate.Linq;
	using NHibernate.Transform;
	using NHibernate.Criterion;

	/// <summary>
	/// Allow programmers to use the 
	/// ActiveRecord functionality without extending ActiveRecordBase/>
	/// </summary>
	public class ActiveRecord<T> where T : class {

		internal static void EnsureInitialized(Type type)
		{
			if (!ActiveRecord.IsInitialized)
			{
				var message = string.Format("An ActiveRecord class ({0}) was used but the framework seems not " +
											   "properly initialized. Did you forget about ActiveRecordStarter.Initialize() ?",
											   type.FullName);
				throw new ActiveRecordException(message);
			}

			var sf = ActiveRecord.Holder.GetSessionFactory(type);

			if (sf.GetClassMetadata(typeof(T)) == null)
			{
				var message = string.Format("You have accessed an ActiveRecord class that wasn't properly initialized. " +
											   "There are two possible explanations: that the call to ActiveRecordStarter.Initialize() didn't include {0} class, or that {0} class is not decorated with the [ActiveRecord] attribute.",
											   type.FullName);
				throw new ActiveRecordException(message);
			}
		}

		/// <summary>
		/// Invokes the specified delegate passing a valid 
		/// NHibernate session. Used for custom NHibernate queries.
		/// </summary>
		/// <param name="func">The delegate instance</param>
		/// <param name="instance">The ActiveRecord instance</param>
		/// <returns>Whatever is returned by the delegate invocation</returns>
		public static TK Execute<TK>(Func<ISession, T, TK> func, T instance) {
			if (func == null) throw new ArgumentNullException("func", "Delegate must be passed");

			EnsureInitialized(typeof(T));

			ISession session = ActiveRecord.Holder.CreateSession(typeof (T));

			try {
				return func(session, instance);

			} catch (ValidationException) {
				ActiveRecord.Holder.FailSession(session);
				throw;

			} catch (ObjectNotFoundException ex) {
				var message = string.Format("Could not find {0} with id {1}", ex.EntityName, ex.Identifier);
				throw new NotFoundException(message, ex);

			} catch (Exception ex) {
				ActiveRecord.Holder.FailSession(session);
				throw new ActiveRecordException("Error performing Execute for " + typeof (T).Name, ex);

			} finally {
				ActiveRecord.Holder.ReleaseSession(session);
			}
		}

		public static void Execute(Action<ISession> action) {
			Execute(session => {
				action(session);
				return string.Empty;
			});
		}

		public static TK Execute<TK>(Func<ISession, TK> func) {
			return Execute((session, arg2) => func(session), null);
		}

		/// <summary>
		/// Finds an object instance by its primary key.
		/// </summary>
		/// <param name="id">ID value</param>
		public static T Find(object id) {
			return Execute(session => session.Get<T>(id));
		}

		/// <summary>
		/// Peeks for an object instance by its primary key,
		/// returns null if not found
		/// </summary>
		/// <param name="id">ID value</param>
		public static T Peek(object id)
		{
			return Execute(session => session.Load<T>(id));
		}

		/// <summary>
		/// Searches and returns the first row.
		/// </summary>
		/// <param name="orders">The sort order - used to determine which record is the first one</param>
		/// <param name="criterias">The criteria expression</param>
		/// <returns>A <c>targetType</c> instance or <c>null</c></returns>
		public static T FindFirst(Order[] orders, params ICriterion[] criterias)
		{
			return SlicedFindAll(0, 1, orders, criterias).FirstOrDefault();
		}

		/// <summary>
		/// Searches and returns the first row.
		/// </summary>
		/// <param name="criterias">The criteria expression</param>
		/// <returns>A <c>targetType</c> instance or <c>null</c></returns>
		public static T FindFirst(params ICriterion[] criterias)
		{
			return SlicedFindAll(0, 1, criterias).FirstOrDefault();
		}

		/// <summary>
		/// Searches and returns the first row.
		/// </summary>
		/// <param name="detachedCriteria">The criteria.</param>
		/// <param name="orders">The sort order - used to determine which record is the first one.</param>
		/// <returns>A <c>targetType</c> instance or <c>null.</c></returns>
		public static T FindFirst(DetachedCriteria detachedCriteria, params Order[] orders)
		{
			return SlicedFindAll(0, 1, detachedCriteria, orders).FirstOrDefault();
		}

		/// <summary>
		/// Searches and returns the first row.
		/// </summary>
		/// <param name="detachedQuery">The expression query.</param>
		/// <returns>A <c>targetType</c> instance or <c>null.</c></returns>
		public static T FindFirst(IDetachedQuery detachedQuery)
		{
			return SlicedFindAll(0, 1, detachedQuery).FirstOrDefault();
		}

		/// <summary>
		/// Searches and returns the first row.
		/// </summary>
		/// <param name="criterias">The criterias.</param>
		/// <returns>A instance the targetType or <c>null</c></returns>
		public static T FindOne(params ICriterion[] criterias)
		{
			var result = SlicedFindAll(0, 2, criterias).ToList();

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
		/// <param name="criteria">The criteria</param>
		/// <returns>A <c>targetType</c> instance or <c>null</c></returns>
		public static T FindOne(DetachedCriteria criteria)
		{
			var result = SlicedFindAll(0, 2, criteria).ToList();

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
		public static T FindOne(IDetachedQuery detachedQuery)
		{
			var result = SlicedFindAll(0, 2, detachedQuery).ToList();

			if (result.Count > 1)
			{
				throw new ActiveRecordException("ActiveRecord.FindOne returned " + result.Count() +
												" rows. Expecting one or none");
			}

			return result.FirstOrDefault();
		}

		/// <summary>
		/// Finds records based on a property value - automatically converts null values to IS NULL style queries. 
		/// </summary>
		/// <param name="property">A property name (not a column name)</param>
		/// <param name="value">The value to be equals to</param>
		/// <returns></returns>
		public static IEnumerable<T> FindAllByProperty(string property, object value)
		{
			ICriterion criteria = (value == null) ? Restrictions.IsNull(property) : Restrictions.Eq(property, value);
			return FindAll(criteria);
		}

		/// <summary>
		/// Finds records based on a property value - automatically converts null values to IS NULL style queries. 
		/// </summary>
		/// <param name="orderByColumn">The column name to be ordered ASC</param>
		/// <param name="property">A property name (not a column name)</param>
		/// <param name="value">The value to be equals to</param>
		/// <returns></returns>
		public static IEnumerable<T> FindAllByProperty(string orderByColumn, string property, object value)
		{
			ICriterion criteria = (value == null) ? Restrictions.IsNull(property) : Restrictions.Eq(property, value);
			return FindAll(new Order[] {Order.Asc(orderByColumn)}, criteria);
		}

		/// <summary>
		/// Returns all instances found for the specified type 
		/// using sort orders and criterias.
		/// </summary>
		/// <param name="orders"></param>
		/// <param name="criterias"></param>
		/// <returns></returns>
		public static IEnumerable<T> FindAll(Order[] orders, params ICriterion[] criterias)
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
		public static IEnumerable<T> FindAll(params ICriterion[] criterias)
		{
			return FindAll(DetachedCriteria.For<T>().SetResultTransformer(Transformers.DistinctRootEntity).AddCriterias(criterias));
		}

		/// <summary>
		/// Returns all instances found for the specified type according to the criteria
		/// </summary>
		public static IEnumerable<T> FindAll(DetachedCriteria detachedCriteria, params Order[] orders)
		{
			return detachedCriteria.AddOrders(orders).List<T>();
		}

		/// <summary>
		/// Returns all instances found for the specified type according to the criteria
		/// </summary>
		/// <param name="detachedQuery">The query expression</param>
		/// <returns>The <see cref="Array"/> of results.</returns>
		public static IEnumerable<T> FindAll(IDetachedQuery detachedQuery)
		{
			return detachedQuery.List<T>();
		}

		/// <summary>
		/// Returns a portion of the query results (sliced)
		/// <param name="firstResult">The number of the first row to retrieve.</param>
		/// <param name="maxResults">The maximum number of results retrieved.</param>
		/// </summary>
		public static IEnumerable<T> SlicedFindAll(int firstResult, int maxResults, Order[] orders, params ICriterion[] criterias)
		{
			return SlicedFindAll(firstResult, maxResults, DetachedCriteria.For<T>().AddCriterias(criterias), orders);
		}

		/// <summary>
		/// Returns a portion of the query results (sliced)
		/// <param name="firstResult">The number of the first row to retrieve.</param>
		/// <param name="maxResults">The maximum number of results retrieved.</param>
		/// </summary>
		public static IEnumerable<T> SlicedFindAll(int firstResult, int maxResults, params ICriterion[] criterias)
		{
			return SlicedFindAll(firstResult, maxResults, DetachedCriteria.For<T>().AddCriterias(criterias));
		}

		/// <summary>
		/// Returns a portion of the query results (sliced)
		/// <param name="firstResult">The number of the first row to retrieve.</param>
		/// <param name="maxResults">The maximum number of results retrieved.</param>
		/// </summary>
		public static IEnumerable<T> SlicedFindAll(int firstResult, int maxResults, DetachedCriteria criteria, params Order[] orders)
		{
			return criteria
				.AddOrders(orders)
				.SetFirstResult(firstResult)
				.SetMaxResults(maxResults)
				.List<T>();
		}

		/// <summary>
		/// Returns a portion of the query results (sliced)
		/// </summary>
		/// <param name="firstResult">The number of the first row to retrieve.</param>
		/// <param name="maxResults">The maximum number of results retrieved.</param>
		/// <param name="detachedQuery">The query expression</param>
		/// <returns>The sliced query results.</returns>
		public static IEnumerable<T> SlicedFindAll(int firstResult, int maxResults, IDetachedQuery detachedQuery)
		{
			return detachedQuery
					.SetFirstResult(firstResult)
					.SetMaxResults(maxResults)
					.List<T>();
		}

		/// <summary>
		/// Returns a portion of the query results (sliced)
		/// </summary>
		/// <param name="firstResult">The number of the first row to retrieve.</param>
		/// <param name="maxResults">The maximum number of results retrieved.</param>
		/// <param name="queryover">Queryover</param>
		/// <returns>The sliced query results.</returns>
		public static IEnumerable<T> SlicedFindAll(int firstResult, int maxResults, QueryOver<T, T> queryover)
		{
			return queryover
					.Skip(firstResult)
					.Take(maxResults)
					.List<T>();
		}

		/// <summary>
		/// Deletes all rows for the specified ActiveRecord type that matches
		/// the supplied criteria
		/// </summary>
		/// <remarks>
		/// This method is usually useful for test cases.
		/// </remarks>
		public static void DeleteAll(DetachedCriteria criteria)
		{
			var pks = criteria.SetProjection(Projections.Id()).List<T, object>();
			DeleteAll(pks);
		}

		/// <summary>
		/// Deletes all rows for the specified ActiveRecord type that matches
		/// the supplied criteria
		/// </summary>
		/// <remarks>
		/// This method is usually useful for test cases.
		/// </remarks>
		public static void DeleteAll(params ICriterion[] criteria) {
			if (criteria != null && criteria.Length > 0)
				DeleteAll(DetachedCriteria.For<T>().AddCriterias(criteria));
			else
				Execute(session => {
					session.CreateQuery("delete from " + typeof(T).FullName)
						.ExecuteUpdate();
					session.Flush();
				});

		}

		public static void DeleteAll(Expression<Func<T, bool>> expression) {
			var pks = QueryOver.Of<T>().Where(expression).Select(Projections.Id()).List<T, object>();
			DeleteAll(pks);
		}

		public static void DeleteAll(QueryOver<T, T> queryover) {
			var pks = queryover.Select(Projections.Id()).List<T, object>();
			DeleteAll(pks);
		}

		/// <summary>
		/// Deletes all rows for the specified ActiveRecord type that matches
		/// the supplied HQL condition
		/// </summary>
		/// <remarks>
		/// This method is usually useful for test cases.
		/// </remarks>
		/// <param name="where">HQL condition to select the rows to be deleted</param>
		public static void DeleteAll(string where)
		{
			Execute(session => {
				session.Delete(string.Format("from {0} where {1}", typeof(T).FullName, where));
				session.Flush();
			});
		}

		/// <summary>
		/// Deletes all rows for the supplied primary keys 
		/// </summary>
		/// <param name="pkvalues">A list of primary keys</param>
		public static void DeleteAll(IEnumerable<object> pkvalues) {
			var cm = ActiveRecord.Holder.GetSessionFactory(typeof (T)).GetClassMetadata(typeof (T));
			var pkname = cm.IdentifierPropertyName;
			var pktype = cm.IdentifierType.ReturnedClass;

			if (pktype == typeof(int) || pktype == typeof(long))
			{
				var hql = "delete from {0} _this where _this.{1} in ({2})";
				Execute(session => {
					session.CreateQuery(string.Format(hql, typeof(T).FullName, pkname, string.Join(",", pkvalues)))
						.ExecuteUpdate();
				});

			}
			else if (pktype == typeof(Guid) || pktype == typeof(string))
			{
				var hql = "delete from {0} _this where _this.{1} in ('{2}')";
				Execute(session => {
					session.CreateQuery(string.Format(hql, typeof(T).FullName, pkname, string.Join("','", pkvalues)))
						.ExecuteUpdate();
				});
				
			}
			else
			{
				Execute(session => {
					foreach (var obj in pkvalues.Select(Peek).Where(o => o != null)) {
						Delete(obj);
					}
				});
			}
		}

		/// <summary>
		/// Provide an IQueryable.
		/// Make sure we are in a scope
		/// </summary>
		public static IQueryable<T> All {
			get {
				if (!ActiveRecord.Holder.ThreadScopeInfo.HasInitializedScope)
					throw new ActiveRecordException("You need to be in an ISessionScope to do linq queries.");

				return Execute(s => s.Query<T>());
			}
		}

		/// <summary>
		/// Saves the instance to the database. If the primary key is unitialized
		/// it creates the instance on the database. Otherwise it updates it.
		/// <para>
		/// If the primary key is assigned, then you must invoke <see cref="Create"/>
		/// or <see cref="Update"/> instead.
		/// </para>
		/// </summary>
		/// <param name="instance">The ActiveRecord instance to be saved</param>
		public static void Save(object instance)
		{
			InternalSave(instance, false);
		}

		/// <summary>
		/// Saves the instance to the database and flushes the session. If the primary key is unitialized
		/// it creates the instance on the database. Otherwise it updates it.
		/// <para>
		/// If the primary key is assigned, then you must invoke <see cref="Create"/>
		/// or <see cref="Update"/> instead.
		/// </para>
		/// </summary>
		/// <param name="instance">The ActiveRecord instance to be saved</param>
		protected internal static void SaveAndFlush(object instance)
		{
			InternalSave(instance, true);
		}

		/// <summary>
		/// Saves the instance to the database. If the primary key is unitialized
		/// it creates the instance on the database. Otherwise it updates it.
		/// <para>
		/// If the primary key is assigned, then you must invoke <see cref="Create"/>
		/// or <see cref="Update"/> instead.
		/// </para>
		/// </summary>
		/// <param name="instance">The ActiveRecord instance to be saved</param>
		/// <param name="flush">if set to <c>true</c>, the operation will be followed by a session flush.</param>
		private static void InternalSave(object instance, bool flush)
		{
			if (instance == null) throw new ArgumentNullException("instance");
			Execute(session => {
				session.SaveOrUpdate(instance);
				if (flush) session.Flush();
			});
		}

		/// <summary>
		/// Saves a copy of the instance to the database. If the primary key is uninitialized
		/// it creates the instance in the database. Otherwise it updates it.
		/// <para>
		/// If the primary key is assigned, then you must invoke <see cref="Create"/>
		/// or <see cref="Update"/> instead.
		/// </para>
		/// </summary>
		/// <param name="instance">The transient instance to be saved</param>
		/// <returns>The saved ActiveRecord instance</returns>
		public static T SaveCopy(T instance)
		{
			return InternalSaveCopy(instance, false);
		}

		/// <summary>
		/// Saves a copy of the instance to the database and flushes the session. If the primary key is uninitialized
		/// it creates the instance in the database. Otherwise it updates it.
		/// <para>
		/// If the primary key is assigned, then you must invoke <see cref="Create"/>
		/// or <see cref="Update"/> instead.
		/// </para>
		/// </summary>
		/// <param name="instance">The transient instance to be saved</param>
		/// <returns>The saved ActiveRecord instance</returns>
		protected internal static T SaveCopyAndFlush(T instance)
		{
			return InternalSaveCopy(instance, true);
		}

		/// <summary>
		/// Saves a copy of the instance to the database. If the primary key is unitialized
		/// it creates the instance on the database. Otherwise it updates it.
		/// <para>
		/// If the primary key is assigned, then you must invoke <see cref="Create"/>
		/// or <see cref="Update"/> instead.
		/// </para>
		/// </summary>
		/// <param name="instance">The transient instance to be saved</param>
		/// <param name="flush">if set to <c>true</c>, the operation will be followed by a session flush.</param>
		/// <returns>The saved ActiveRecord instance.</returns>
		private static T InternalSaveCopy(T instance, bool flush)
		{
		if (instance == null) throw new ArgumentNullException("instance");
			return Execute(session => {
			  T persistent = session.Merge(instance);
			  if (flush) session.Flush();
			  return persistent;
			});
		}

		/// <summary>
		/// Creates (Saves) a new instance to the database.
		/// </summary>
		/// <param name="instance"></param>
		public static void Create(object instance)
		{
			InternalCreate(instance, false);
		}

		/// <summary>
		/// Creates (Saves) a new instance to the database.
		/// </summary>
		/// <param name="instance"></param>
		public static void CreateAndFlush(object instance)
		{
			InternalCreate(instance, false);
		}

		/// <summary>
		/// Creates (Saves) a new instance to the database.
		/// </summary>
		/// <param name="instance">The ActiveRecord instance to be updated on the database</param>
		/// <param name="flush">if set to <c>true</c>, the operation will be followed by a session flush.</param>
		private static void InternalCreate(object instance, bool flush)
		{
			if (instance == null) throw new ArgumentNullException("instance");
			Execute(session => {
				session.Save(instance);
				if (flush) session.Flush();
			});
		}

		/// <summary>
		/// Persists the modification on the instance
		/// state to the database.
		/// </summary>
		/// <param name="instance"></param>
		public static void Update(object instance)
		{
			InternalUpdate(instance, false);
		}

		/// <summary>
		/// Persists the modification on the instance
		/// state to the database and flushes the session.
		/// </summary>
		/// <param name="instance">The ActiveRecord instance to be updated on the database</param>
		public static void UpdateAndFlush(object instance)
		{
			InternalUpdate(instance, true);
		}

		/// <summary>
		/// Persists the modification on the instance
		/// state to the database.
		/// </summary>
		/// <param name="instance">The ActiveRecord instance to be updated on the database</param>
		/// <param name="flush">if set to <c>true</c>, the operation will be followed by a session flush.</param>
		private static void InternalUpdate(object instance, bool flush)
		{
			if (instance == null) throw new ArgumentNullException("instance");
			Execute(session => {
				session.Update(instance);

				if (flush) session.Flush();
			});
		}

		/// <summary>
		/// Deletes the instance from the database.
		/// </summary>
		/// <param name="instance">The ActiveRecord instance to be deleted</param>
		public static void Delete(object instance)
		{
			InternalDelete(instance, false);
		}

		/// <summary>
		/// Deletes the instance from the database and flushes the session.
		/// </summary>
		/// <param name="instance">The ActiveRecord instance to be deleted</param>
		public static void DeleteAndFlush(object instance)
		{
			InternalDelete(instance, true);
		}

		/// <summary>
		/// Deletes the instance from the database.
		/// </summary>
		/// <param name="instance">The ActiveRecord instance to be deleted</param>
		/// <param name="flush">if set to <c>true</c>, the operation will be followed by a session flush.</param>
		private static void InternalDelete(object instance, bool flush)
		{
			if (instance == null) throw new ArgumentNullException("instance");
			Execute(session => {
				session.Delete(instance);
				if (flush) session.Flush();
			});
		}

		/// <summary>
		/// Refresh the instance from the database.
		/// </summary>
		/// <param name="instance">The ActiveRecord instance to be reloaded</param>
		protected internal static void Refresh(object instance)
		{
			if (instance == null) throw new ArgumentNullException("instance");
			Execute(session => session.Refresh(instance));
		}

		/// <summary>
		/// Merge the instance to scope session
		/// </summary>
		/// <param name="instance"></param>
		public static void Merge(object instance) {
			if (instance == null) throw new ArgumentNullException("instance");
			Execute(session => session.Merge(instance));
		}

		/// <summary>
		/// Evict the instance from scope session
		/// </summary>
		/// <param name="instance"></param>
		public static void Evict(object instance) {
			if (instance == null) throw new ArgumentNullException("instance");
			Execute(session => session.Evict(instance));
		}

		/// <summary>
		/// From NHibernate documentation: 
		/// Persist all reachable transient objects, reusing the current identifier 
		/// values. Note that this will not trigger the Interceptor of the Session.
		/// </summary>
		/// <param name="instance">The instance.</param>
		/// <param name="replicationMode">The replication mode.</param>
		public static void Replicate(T instance, ReplicationMode replicationMode)
		{
			if (instance == null) { throw new ArgumentNullException("instance"); }
			Execute(session => session.Replicate(instance, replicationMode));
		}

		/// <summary>
		/// Check if any instance matches the query.
		/// </summary>
		/// <param name="detachedQuery">The query expression</param>
		/// <returns><c>true</c> if an instance is found; otherwise <c>false</c>.</returns>
		public static bool Exists(IDetachedQuery detachedQuery)
		{
			return SlicedFindAll(0, 1, detachedQuery).Any();
		}

		/// <summary>
		/// Returns the number of records of the specified 
		/// type in the database that match the given critera
		/// </summary>
		/// <param name="criteria">The criteria expression</param>
		/// <returns>The count result</returns>
		public static int Count(params ICriterion[] criteria)
		{
			var dc = DetachedCriteria.For<T>()
				.AddCriterias(criteria)
				.SetProjection(Projections.RowCount());

			return Count(dc);
		}

		/// <summary>
		/// Returns the number of records of the specified 
		/// type in the database
		/// </summary>
		/// <param name="detachedCriteria">The criteria expression</param>
		/// <returns>The count result</returns>
		public static int Count(DetachedCriteria detachedCriteria)
		{
			return Execute(session => detachedCriteria.GetExecutableCriteria(session).UniqueResult<int>());
		}

		/// <summary>
		/// Check if the <paramref name="id"/> exists in the database.
		/// </summary>
		/// <param name="id">The id to check on</param>
		/// <returns><c>true</c> if the ID exists; otherwise <c>false</c>.</returns>
		public static bool Exists(object id)
		{
			return Execute(session => session.Get<T>(id) != null);
		}

		/// <summary>
		/// Check if any instance matches the criteria.
		/// </summary>
		/// <returns><c>true</c> if an instance is found; otherwise <c>false</c>.</returns>
		public static bool Exists(params ICriterion[] criterias)
		{
			return Count(criterias) > 0;
		}

		/// <summary>
		/// Check if any instance matching the criteria exists in the database.
		/// </summary>
		/// <param name="detachedCriteria">The criteria expression</param>
		/// <returns><c>true</c> if an instance is found; otherwise <c>false</c>.</returns>
		public static bool Exists(DetachedCriteria detachedCriteria)
		{
			return Count(detachedCriteria) > 0;
		}

		/// <summary>
		/// Check if there is any records in the db for the target type
		/// </summary>
		/// <param name="targetType">The target type.</param>
		/// <param name="detachedQuery"></param>
		/// <returns><c>true</c> if there's at least one row</returns>
		protected internal static bool Exists(Type targetType, IDetachedQuery detachedQuery)
		{
			return SlicedFindAll(0, 1, detachedQuery).Any();
		}
	}
}
