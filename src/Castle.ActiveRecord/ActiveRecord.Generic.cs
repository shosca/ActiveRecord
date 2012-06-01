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

using Castle.Components.Validator;
using NHibernate.Linq;

namespace Castle.ActiveRecord
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	using NHibernate;
	using NHibernate.Transform;
	using NHibernate.Criterion;

	/// <summary>
	/// Allow programmers to use the 
	/// ActiveRecord functionality without extending ActiveRecordBase/>
	/// </summary>
	public class ActiveRecord<T> where T : class {

		internal static void EnsureInitialized()
		{
			if (!ActiveRecord.IsInitialized)
			{
				String message = String.Format("An ActiveRecord class ({0}) was used but the framework seems not " +
											   "properly initialized. Did you forget about ActiveRecordStarter.Initialize() ?",
											   typeof(T).FullName);
				throw new ActiveRecordException(message);
			}
			ISessionFactory sf = ActiveRecord.Holder.GetSessionFactory(typeof (T));
			if (sf.GetClassMetadata(typeof(T)) == null)
			{
				String message = String.Format("You have accessed an ActiveRecord class that wasn't properly initialized. " +
											   "There are two possible explanations: that the call to ActiveRecordStarter.Initialize() didn't include {0} class, or that {0} class is not decorated with the [ActiveRecord] attribute.",
											   typeof(T).FullName);
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

			EnsureInitialized();

			ISession session = ActiveRecord.Holder.CreateSession(typeof (T));

			try {
				return func(session, instance);

			} catch (ValidationException) {
				ActiveRecord.Holder.FailSession(session);
				throw;

			} catch (ObjectNotFoundException ex) {
				String message = String.Format("Could not find {0} with id {1}", ex.EntityName, ex.Identifier);
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
			bool hasScope = ActiveRecord.Holder.ThreadScopeInfo.HasInitializedScope;
			T entity = null;
			Execute(session => {
				entity = session.Get<T>(id);
				// If not in a scope, eager load entity
				if (!hasScope) NHibernateUtil.Initialize(entity);
			});
			return entity;
		}

		/// <summary>
		/// Peeks for an object instance by its primary key,
		/// returns null if not found
		/// </summary>
		/// <param name="id">ID value</param>
		public static T Peek(object id)
		{
			bool hasScope = ActiveRecord.Holder.ThreadScopeInfo.HasInitializedScope;
			T entity = null;
			Execute(session => {
				entity = session.Load<T>(id);
				// If not in a scope, eager load entity
				if (!hasScope) NHibernateUtil.Initialize(entity);
			});
			return entity;
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
		/// <param name="criteria">The criteria expression</param>
		/// <returns>A <c>targetType</c> instance or <c>null</c></returns>
		public static T FindFirst(DetachedCriteria criteria)
		{
			return SlicedFindAll(0, 1, criteria).FirstOrDefault();
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
			IEnumerable<T> result = SlicedFindAll(0, 2, criterias);

			if (result.Count() > 1)
			{
				throw new ActiveRecordException(typeof(T).Name + ".FindOne returned " + result.Count() +
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
			IEnumerable<T> result = SlicedFindAll(0, 2, criteria);

			if (result.Count() > 1)
			{
				throw new ActiveRecordException(typeof(T).Name + ".FindOne returned " + result.Count() +
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
			IEnumerable<T> result = SlicedFindAll(0, 2, detachedQuery);

			if (result.Count() > 1)
			{
				throw new ActiveRecordException(typeof(T).Name + ".FindOne returned " + result.Count() +
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
		public static IEnumerable<T> FindAllByProperty(String property, object value)
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
		public static IEnumerable<T> FindAllByProperty(String orderByColumn, String property, object value)
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
			EnsureInitialized();
			return Execute(session => {
				ICriteria sessionCriteria = session.CreateCriteria<T>();

				foreach(ICriterion cond in criterias)
				{
					sessionCriteria.Add(cond);
				}

				AddOrdersToCriteria(sessionCriteria, orders);

				return sessionCriteria.List<T>();
			});
		}

		/// <summary>
		/// Returns all instances found for the specified type 
		/// using criterias.
		/// </summary>
		/// <param name="criterias"></param>
		/// <returns></returns>
		public static IEnumerable<T> FindAll(params ICriterion[] criterias)
		{
			if (criterias == null) {
				return FindAll(DetachedCriteria.For<T>().SetResultTransformer(Transformers.DistinctRootEntity));
			} 
			return FindAll(null, criterias);
		}

		/// <summary>
		/// Returns all instances found for the specified type according to the criteria
		/// </summary>
		public static IEnumerable<T> FindAll(DetachedCriteria detachedCriteria, params Order[] orders)
		{
			EnsureInitialized();
			return Execute(session =>
			{
				ICriteria criteria = detachedCriteria.GetExecutableCriteria(session);

				AddOrdersToCriteria(criteria, orders);

				return criteria.List<T>();

			});
		}

		private static void AddOrdersToCriteria(ICriteria criteria, IEnumerable<Order> orders)
		{
			if (orders != null)
			{
				foreach (Order order in orders)
				{
					criteria.AddOrder(order);
				}
			}
		}

		/// <summary>
		/// Returns all instances found for the specified type according to the criteria
		/// </summary>
		/// <param name="detachedQuery">The query expression</param>
		/// <returns>The <see cref="Array"/> of results.</returns>
		public static IEnumerable<T> FindAll(IDetachedQuery detachedQuery)
		{
			EnsureInitialized();
			return Execute(session => detachedQuery.GetExecutableQuery(session).List<T>());
		}

		/// <summary>
		/// Returns a portion of the query results (sliced)
		/// </summary>
		public static IEnumerable<T> SlicedFindAll(int firstResult, int maxResults, Order[] orders, params ICriterion[] criterias)
		{
			EnsureInitialized();
			return Execute(session => {
				ICriteria sessionCriteria = session.CreateCriteria(typeof(T));

				foreach(ICriterion cond in criterias)
				{
					sessionCriteria.Add(cond);
				}

				if (orders != null)
				{
					foreach (Order order in orders)
					{
						sessionCriteria.AddOrder(order);
					}
				}

				sessionCriteria.SetFirstResult(firstResult);
				sessionCriteria.SetMaxResults(maxResults);

				return sessionCriteria.List<T>();
			});
		}

		/// <summary>
		/// Returns a portion of the query results (sliced)
		/// </summary>
		public static IEnumerable<T> SlicedFindAll(int firstResult, int maxResults, params ICriterion[] criterias)
		{
			return SlicedFindAll(firstResult, maxResults, null, criterias);
		}

		/// <summary>
		/// Returns a portion of the query results (sliced)
		/// </summary>
		public static IEnumerable<T> SlicedFindAll(int firstResult, int maxResults, DetachedCriteria criteria, params Order[] orders)
		{
			return Execute(session =>
			{
				ICriteria executableCriteria = criteria.GetExecutableCriteria(session);
				AddOrdersToCriteria(executableCriteria, orders);
				executableCriteria.SetFirstResult(firstResult);
				executableCriteria.SetMaxResults(maxResults);

				return executableCriteria.List<T>();
			});
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
			return Execute(session =>
			{
				IQuery executableQuery = detachedQuery.GetExecutableQuery(session);
				executableQuery.SetFirstResult(firstResult);
				executableQuery.SetMaxResults(maxResults);
				return executableQuery.List<T>();
			});
		}

		/// <summary>
		/// Deletes all rows for the specified ActiveRecord type that matches
		/// the supplied HQL condition
		/// </summary>
		/// <remarks>
		/// This method is usually useful for test cases.
		/// </remarks>
		public static void DeleteAll()
		{
			Execute(session => {
				session.Delete("from " + typeof(T).FullName);
				session.Flush();
			});
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
				session.Delete(String.Format("from {0} where {1}", typeof(T).FullName, where));
				session.Flush();
			});
		}

		/// <summary>
		/// Deletes all rows for the supplied primary keys 
		/// </summary>
		/// <param name="pkvalues">A list of primary keys</param>
		public static int DeleteAll(IEnumerable pkvalues) {
			return Execute(session => {
				int counter = 0;
				foreach (var pkvalue in pkvalues) {
					T obj = Peek(pkvalue);
					if (obj != null) {
						Delete(obj);
						counter++;
					}
				}
				return counter;
			});
				
		}

		/// <summary>
		/// Provide an IQueryable.
		/// Make sure we are in a scope
		/// </summary>
		public static IQueryable<T> All {
			get {
				if (ActiveRecord.Holder.ThreadScopeInfo.HasInitializedScope)
					throw new ActiveRecordException("You need to be in an IScope to do linq queries.");

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
			DetachedCriteria dc = DetachedCriteria.For<T>();
			foreach (var criterion in criteria) {
				dc.Add(criterion);
			}
			dc.SetProjection(Projections.RowCount());
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

	public static class QueryOverExtensions {
		public static IEnumerable<T> List<T>(this QueryOver<T> query) where T : class
		{
			return ActiveRecord<T>.Execute(session => query.GetExecutableQueryOver(session).List<T>());
		}

		public static IEnumerable<TR> List<T, TR>(this QueryOver<T> query) where T : class
		{
			return ActiveRecord<T>.Execute(session => query.GetExecutableQueryOver(session).List<TR>());
		}
	}
}
