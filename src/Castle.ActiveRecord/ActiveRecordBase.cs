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
using System.Linq;
using System.Linq.Expressions;
using NHibernate.Linq;

namespace Castle.ActiveRecord
{
	using System;
	using System.Collections;
	using NHibernate;
	using NHibernate.Criterion;

	/// <summary>
	/// Base class for all ActiveRecord Generic classes. 
	/// Implements all the functionality to simplify the code on the subclasses.
	/// </summary>
	[Serializable]
	public abstract class ActiveRecordBase<T> : ActiveRecordHooksBase where T : class
	{
		#region Find/Peek

		/// <summary>
		/// Finds an object instance by an unique ID 
		/// </summary>
		/// <param name="id">ID value</param>
		/// <exception cref="ObjectNotFoundException">if the row is not found</exception>
		/// <returns>T</returns>
		public static T Find(object id)
		{
			return ActiveRecord<T>.Find(id);
		}

		/// <summary>
		/// Finds an object instance by an unique ID.
		/// If the row is not found this method will return null
		/// </summary>
		/// <param name="id">ID value</param>
		/// <returns>A <typeparamref name="T"/></returns>
		public static T Peek(object id)
		{
			return ActiveRecord<T>.Peek(id);
		}

		#endregion

		#region Exists/Count

		/// <summary>
		/// Check if the <paramref name="id"/> exists in the database.
		/// </summary>
		/// <param name="id">The id to check on</param>
		/// <returns><c>true</c> if the ID exists; otherwise <c>false</c>.</returns>
		public static bool Exists(object id)
		{
			return ActiveRecord<T>.Exists(id);
		}

		/// <summary>
		/// Check if any instance matches the query.
		/// </summary>
		/// <param name="detachedQuery">The query expression</param>
		/// <returns><c>true</c> if an instance is found; otherwise <c>false</c>.</returns>
		public static bool Exists(IDetachedQuery detachedQuery)
		{
			return ActiveRecord<T>.Exists(detachedQuery);
		}

		/// <summary>
		/// Check if any instance matches the criteria.
		/// </summary>
		/// <returns><c>true</c> if an instance is found; otherwise <c>false</c>.</returns>
		public static bool Exists(params ICriterion[] criterias)
		{
			return ActiveRecord<T>.Exists(criterias);
		}

		/// <summary>
		/// Check if any instance matching the criteria exists in the database.
		/// </summary>
		/// <param name="expression">The queryover expression</param>
		/// <returns><c>true</c> if an instance is found; otherwise <c>false</c>.</returns>
		public static bool Exists(Expression<Func<T, bool>> expression)
		{
			return ActiveRecord<T>.Exists(expression);
		}

		/// <summary>
		/// Check if any instance matching the criteria exists in the database.
		/// </summary>
		/// <param name="queryover">The queryover expression</param>
		/// <returns><c>true</c> if an instance is found; otherwise <c>false</c>.</returns>
		public static bool Exists(QueryOver<T, T> queryover)
		{
			return ActiveRecord<T>.Exists(queryover);
		}

		/// <summary>
		/// Check if any instance matching the criteria exists in the database.
		/// </summary>
		/// <param name="detachedCriteria">The criteria expression</param>
		/// <returns><c>true</c> if an instance is found; otherwise <c>false</c>.</returns>
		public static bool Exists(DetachedCriteria detachedCriteria)
		{
			return ActiveRecord<T>.Exists(detachedCriteria);
		}

		/// <summary>
		/// Returns the number of records of the specified 
		/// type in the database that match the given critera
		/// </summary>
		/// <param name="criteria">The criteria expression</param>
		/// <returns>The count result</returns>
		public static int Count(params ICriterion[] criteria)
		{
			return ActiveRecord<T>.Count(criteria);
		}

		/// <summary>
		/// Returns the number of records of the specified 
		/// type in the database
		/// </summary>
		/// <param name="expression">The criteria expression</param>
		/// <returns>The count result</returns>
		public static int Count(Expression<Func<T, bool>> expression)
		{
			return ActiveRecord<T>.Count(expression);
		}

		/// <summary>
		/// Returns the number of records of the specified 
		/// type in the database
		/// </summary>
		/// <param name="queryover">The criteria expression</param>
		/// <returns>The count result</returns>
		public static int Count(QueryOver<T, T> queryover)
		{
			return ActiveRecord<T>.Count(queryover);
		}

		/// <summary>
		/// Returns the number of records of the specified 
		/// type in the database
		/// </summary>
		/// <param name="detachedCriteria">The criteria expression</param>
		/// <returns>The count result</returns>
		public static int Count(DetachedCriteria detachedCriteria)
		{
			return ActiveRecord<T>.Count(detachedCriteria);
		}

		#endregion

		#region FindFirst

		/// <summary>
		/// Searches and returns the first row for <typeparamref name="T"/>
		/// </summary>
		/// <param name="order">The sort order - used to determine which record is the first one</param>
		/// <param name="criteria">The criteria expression</param>
		/// <returns>A <c>targetType</c> instance or <c>null</c></returns>
		public static T FindFirst(Order order, params ICriterion[] criteria)
		{
			return ActiveRecord<T>.FindFirst(order, criteria);
		}

		/// <summary>
		/// Searches and returns the first row for <typeparamref name="T"/>
		/// </summary>
		/// <param name="orders">The sort order - used to determine which record is the first one</param>
		/// <param name="criteria">The criteria expression</param>
		/// <returns>A <c>targetType</c> instance or <c>null</c></returns>
		public static T FindFirst(Order[] orders, params ICriterion[] criteria)
		{
			return ActiveRecord<T>.FindFirst(orders, criteria);
		}

		/// <summary>
		/// Searches and returns the first row for <typeparamref name="T"/>
		/// </summary>
		/// <param name="criteria">The criteria expression</param>
		/// <returns>A <c>targetType</c> instance or <c>null</c></returns>
		public static T FindFirst(params ICriterion[] criteria)
		{
			return ActiveRecord<T>.FindFirst(criteria);
		}


		/// <summary>
		/// Searches and returns the first row for <typeparamref name="T"/>.
		/// </summary>
		/// <param name="criteria">Detached criteria.</param>
		/// <param name="orders">The sort order - used to determine which record is the first one.</param>
		/// <returns>A <c>targetType</c> instance or <c>null</c>.</returns>
		public static T FindFirst(DetachedCriteria criteria, params Order[] orders)
		{
			return ActiveRecord<T>.FindFirst(criteria, orders);
		}
		/// <summary>
		/// Searches and returns the first row. 
		/// </summary>
		/// <param name="detachedQuery">The query expression</param>
		/// <returns>A <c>targetType</c> instance or <c>null.</c></returns>
		public static T FindFirst(IDetachedQuery detachedQuery) 
		{
			return ActiveRecord<T>.FindFirst(detachedQuery);
		}

		#endregion

		#region FindOne

		/// <summary>
		/// Searches and returns a row. If more than one is found, 
		/// throws <see cref="ActiveRecordException"/>
		/// </summary>
		/// <param name="criteria">The criteria expression</param>
		/// <returns>A <c>targetType</c> instance or <c>null</c></returns>
		public static T FindOne(params ICriterion[] criteria)
		{
			return ActiveRecord<T>.FindOne(criteria);
		}
		/// <summary>
		/// Searches and returns a row. If more than one is found, 
		/// throws <see cref="ActiveRecordException"/>
		/// </summary>
		/// <param name="queryover">The QueryOver</param>
		/// <returns>A <c>targetType</c> instance or <c>null</c></returns>
		public static T FindOne(QueryOver<T,T> queryover) {
			return ActiveRecord<T>.FindOne(queryover);
		}

		/// <summary>
		/// Searches and returns a row. If more than one is found, 
		/// throws <see cref="ActiveRecordException"/>
		/// </summary>
		/// <param name="criteria">The criteria</param>
		/// <returns>A <c>targetType</c> instance or <c>null</c></returns>
		public static T FindOne(DetachedCriteria criteria)
		{
			return ActiveRecord<T>.FindOne(criteria);
		}

		/// <summary>
		/// Searches and returns a row. If more than one is found, 
		/// throws <see cref="ActiveRecordException"/>
		/// <param name="detachedQuery">The query expression</param>
		/// </summary>
		/// <returns>A <c>targetType</c> instance or <c>null</c></returns>
		public static T FindOne(IDetachedQuery detachedQuery) 
		{
			return ActiveRecord<T>.FindOne(detachedQuery);
		}

		#endregion

		#region FindAllByProperty

		/// <summary>
		/// Finds records based on a property value
		/// </summary>
		/// <param name="property">A property name (not a column name)</param>
		/// <param name="value">The value to be equals to</param>
		/// <returns>An <see cref="Array"/> of <typeparamref name="T"/></returns>
		public static IEnumerable<T> FindAllByProperty(string property, object value)
		{
			return ActiveRecord<T>.FindAllByProperty(property, value);
		}

		/// <summary>
		/// Finds records based on a property value
		/// </summary>
		/// <param name="orderByColumn">The column name to be ordered ASC</param>
		/// <param name="property">A property name (not a column name)</param>
		/// <param name="value">The value to be equals to</param>
		/// <returns>An <see cref="Array"/> of <typeparamref name="T"/></returns>
		public static IEnumerable<T> FindAllByProperty(string orderByColumn, string property, object value)
		{
			return ActiveRecord<T>.FindAllByProperty(orderByColumn, property, value);
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
		public static IEnumerable<T> FindAll(Order order, params ICriterion[] criteria)
		{
			return ActiveRecord<T>.FindAll(order, criteria);
		}

		/// <summary>
		/// Returns all instances found for <typeparamref name="T"/>
		/// using sort orders and criteria.
		/// </summary>
		/// <param name="orders"></param>
		/// <param name="criteria"></param>
		/// <returns>An <see cref="Array"/> of <typeparamref name="T"/></returns>
		public static IEnumerable<T> FindAll(Order[] orders, params ICriterion[] criteria)
		{
			return ActiveRecord<T>.FindAll(orders, criteria);
		}

		/// <summary>
		/// Returns all instances found for <typeparamref name="T"/>
		/// using criteria.
		/// </summary>
		/// <param name="criteria"></param>
		/// <returns>An <see cref="Array"/> of <typeparamref name="T"/></returns>
		public static IEnumerable<T> FindAll(params ICriterion[] criteria)
		{
			return ActiveRecord<T>.FindAll(criteria);
		}

		/// <summary>
		/// Returns all the instances that match the detached queryover.
		/// </summary>
		/// <param name="queryover">Detached criteria</param>
		/// <returns>All entities that match the criteria</returns>
		public static IEnumerable<T> FindAll(QueryOver<T, T> queryover)
		{
			return ActiveRecord<T>.FindAll(queryover);
		}


		/// <summary>
		/// Returns all the instances that match the detached criteria.
		/// </summary>
		/// <param name="criteria">Detached criteria</param>
		/// <param name="orders">Optional ordering</param>
		/// <returns>All entities that match the criteria</returns>
		public static IEnumerable<T> FindAll(DetachedCriteria criteria, params Order[] orders)
		{
			return ActiveRecord<T>.FindAll(criteria, orders);
		}

		/// <summary>
		/// Returns all instances found for the specified type according to the criteria
		/// </summary>
		/// <param name="detachedQuery">The query expression.</param>
		/// <returns>All entities that match the query</returns>
		public static IEnumerable<T> FindAll(IDetachedQuery detachedQuery)
		{
			return ActiveRecord<T>.FindAll(detachedQuery);
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
		public static IEnumerable<T> SlicedFindAll(int firstResult, int maxResults, Order order, params ICriterion[] criteria)
		{
			return ActiveRecord<T>.SlicedFindAll(firstResult, maxResults, order, criteria);
		}


		/// <summary>
		/// Returns a portion of the query results (sliced)
		/// </summary>
		/// <param name="firstResult">The number of the first row to retrieve.</param>
		/// <param name="maxResults">The maximum number of results retrieved.</param>
		/// <param name="orders">An <see cref="Array"/> of <see cref="Order"/> objects.</param>
		/// <param name="criteria">The criteria expression</param>
		/// <returns>The sliced query results.</returns>
		public static IEnumerable<T> SlicedFindAll(int firstResult, int maxResults, Order[] orders, params ICriterion[] criteria)
		{
			return ActiveRecord<T>.SlicedFindAll(firstResult, maxResults, orders, criteria);
		}

		/// <summary>
		/// Returns a portion of the query results (sliced)
		/// </summary>
		/// <param name="firstResult">The number of the first row to retrieve.</param>
		/// <param name="maxResults">The maximum number of results retrieved.</param>
		/// <param name="criteria">The criteria expression</param>
		/// <returns>The sliced query results.</returns>
		public static IEnumerable<T> SlicedFindAll(int firstResult, int maxResults, params ICriterion[] criteria)
		{
			return ActiveRecord<T>.SlicedFindAll(firstResult, maxResults, criteria);
		}

		/// <summary>
		/// Returns a portion of the query results (sliced)
		/// </summary>
		/// <param name="firstResult">The number of the first row to retrieve.</param>
		/// <param name="maxResults">The maximum number of results retrieved.</param>
		/// <param name="orders">An <see cref="Array"/> of <see cref="Order"/> objects.</param>
		/// <param name="criteria">The criteria expression</param>
		/// <returns>The sliced query results.</returns>
		public static IEnumerable<T> SlicedFindAll(int firstResult, int maxResults, DetachedCriteria criteria, params Order[] orders)
		{
			return ActiveRecord<T>.SlicedFindAll(firstResult, maxResults, criteria, orders);
		}

		/// <summary>
		/// Returns a portion of the query results (sliced)
		/// </summary>
		/// <param name="firstResult">The number of the first row to retrieve.</param>
		/// <param name="maxResults">The maximum number of results retrieved.</param>
		/// <returns>The sliced query results.</returns>
		/// <param name="detachedQuery">The query expression</param>
		public static IEnumerable<T> SlicedFindAll(int firstResult, int maxResults, IDetachedQuery detachedQuery)
		{
			return ActiveRecord<T>.SlicedFindAll(firstResult, maxResults, detachedQuery);
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
			return ActiveRecord<T>.SlicedFindAll(firstResult, maxResults, queryover);
		}

		#endregion

		#region DeleteAll

		/// <summary>
		/// Deletes all rows for the specified ActiveRecord type that matches
		/// the supplied criteria
		/// </summary>
		public static void DeleteAll(DetachedCriteria criteria)
		{
			ActiveRecord<T>.DeleteAll(criteria);
		}

		/// <summary>
		/// Deletes all rows for the specified ActiveRecord type that matches
		/// the supplied criteria
		/// </summary>
		public static void DeleteAll(params ICriterion[] criteria) {
			ActiveRecord<T>.DeleteAll(criteria);
		}

		/// <summary>
		/// Deletes all rows for the specified ActiveRecord type that matches
		/// the supplied expression criteria
		/// </summary>
		public static void DeleteAll(Expression<Func<T, bool>> expression) {
			ActiveRecord<T>.DeleteAll(expression);
		}

		/// <summary>
		/// Deletes all rows for the specified ActiveRecord type that matches
		/// the supplied queryover
		/// </summary>
		public static void DeleteAll(QueryOver<T, T> queryover)
		{
			ActiveRecord<T>.DeleteAll(queryover);
		}

		/// <summary>
		/// Deletes all rows for the specified ActiveRecord type that matches
		/// the supplied HQL condition
		/// </summary>
		/// <param name="where">HQL condition to select the rows to be deleted</param>
		public static void DeleteAll(string where)
		{
			ActiveRecord<T>.DeleteAll(where);
		}

		/// <summary>
		/// Deletes all <typeparamref name="T"/> objects, based on the primary keys
		/// supplied on <paramref name="pkValues" />.
		/// </summary>
		/// <returns>The number of objects deleted</returns>
		public static void DeleteAll(IEnumerable<object> pkValues)
		{
			ActiveRecord<T>.DeleteAll(pkValues);
		}

		#endregion

		#region Instance

		/// <summary>
		/// Saves the instance to the database. If the primary key is unitialized
		/// it creates the instance on the database. Otherwise it updates it.
		/// <para>
		/// If the primary key is assigned, then you must invoke <see cref="Create"/>
		/// or <see cref="Update"/> instead.
		/// </para>
		/// </summary>
		public virtual void Save() {
			ActiveRecord<T>.Save(this);
		}

		/// <summary>
		/// Saves the instance to the database and flushes the session. If the primary key is unitialized
		/// it creates the instance on the database. Otherwise it updates it.
		/// <para>
		/// If the primary key is assigned, then you must invoke <see cref="Create"/>
		/// or <see cref="Update"/> instead.
		/// </para>
		/// </summary>
		public virtual void SaveAndFlush() {
			ActiveRecord<T>.SaveAndFlush(this);
		}

		/// <summary>
		/// Saves a copy of the instance to the database. If the primary key is uninitialized
		/// it creates the instance in the database. Otherwise it updates it.
		/// <para>
		/// If the primary key is assigned, then you must invoke <see cref="Create"/>
		/// or <see cref="Update"/> instead.
		/// </para>
		/// </summary>
		/// <returns>The saved ActiveRecord instance</returns>
		public virtual T SaveCopy()
		{
			return ActiveRecord<T>.SaveCopy(this);
		}

		/// <summary>
		/// Saves a copy of the instance to the database and flushes the session. If the primary key is uninitialized
		/// it creates the instance in the database. Otherwise it updates it.
		/// <para>
		/// If the primary key is assigned, then you must invoke <see cref="Create"/>
		/// or <see cref="Update"/> instead.
		/// </para>
		/// </summary>
		/// <returns>The saved ActiveRecord instance</returns>
		public virtual T SaveCopyAndFlush()
		{
			return ActiveRecord<T>.SaveCopyAndFlush(this);
		}

		/// <summary>
		/// Creates (Saves) a new instance to the database.
		/// </summary>
		public virtual void Create() {
			ActiveRecord<T>.Create(this);
		}

		/// <summary>
		/// Creates (Saves) a new instance to the database.
		/// </summary>
		public virtual void CreateAndFlush() {
			ActiveRecord<T>.CreateAndFlush(this);
		}

		/// <summary>
		/// Persists the modification on the instance
		/// state to the database.
		/// </summary>
		public virtual void Update() {
			ActiveRecord<T>.Update(this);
		}

		/// <summary>
		/// Persists the modification on the instance
		/// state to the database and flushes the session.
		/// </summary>
		public virtual void UpdateAndFlush() {
			ActiveRecord<T>.UpdateAndFlush(this);
		}


		/// <summary>
		/// Deletes the instance from the database.
		/// </summary>
		public virtual void Delete() {
			ActiveRecord<T>.Delete(this);
		}

		/// <summary>
		/// Deletes the instance from the database and flushes the session.
		/// </summary>
		public virtual void DeleteAndFlush() {
			ActiveRecord<T>.DeleteAndFlush(this);
		}

		/// <summary>
		/// Refresh the instance from the database.
		/// </summary>
		public virtual void Refresh() {
			ActiveRecord<T>.Refresh(this);
		}

		/// <summary>
		/// Merge the instance to scope session
		/// </summary>
		public virtual void Merge() {
			ActiveRecord<T>.Merge(this);
		}

		/// <summary>
		/// Evict the instance from scope session
		/// </summary>
		public virtual void Evict() {
			ActiveRecord<T>.Evict(this);
		}

		/// <summary>
		/// From NHibernate documentation: 
		/// Persist all reachable transient objects, reusing the current identifier 
		/// values. Note that this will not trigger the Interceptor of the Session.
		/// </summary>
		/// <param name="replicationMode">The replication mode.</param>
		public virtual void Replicate(ReplicationMode replicationMode)
		{
			ActiveRecord<T>.Replicate(this, replicationMode);
		}

		#endregion

		#region Linq/QueryOver

		/// <summary>
		/// Provide an IQueryable.
		/// Make sure we are in a scope
		/// </summary>
		public static IQueryable<T> All {
			get { return ActiveRecord<T>.All; }
		}

		/// <summary>
		/// The QueryOver method is used as a Linq collection
		/// or as the in argument in a Linq expression. 
		/// </summary>
		/// <remarks>You must have an open Session Scope.</remarks>
		public static QueryOver<T,T> QueryOver() {
			return ActiveRecord<T>.QueryOver();
		}

		/// <summary>
		/// The QueryOver method is used as a Linq collection
		/// or as the in argument in a Linq expression. 
		/// </summary>
		/// <remarks>You must have an open Session Scope.</remarks>
		public static QueryOver<T,T> QueryOver(Expression<Func<T>> alias) {
			return ActiveRecord<T>.QueryOver(alias);
		}

		/// <summary>
		/// The QueryOver method is used as a Linq collection
		/// or as the in argument in a Linq expression. 
		/// </summary>
		/// <remarks>You must have an open Session Scope.</remarks>
		public static QueryOver<T,T> QueryOver(string entityname) {
			return ActiveRecord<T>.QueryOver(entityname);
		}

		/// <summary>
		/// The QueryOver method is used as a Linq collection
		/// or as the in argument in a Linq expression. 
		/// </summary>
		/// <remarks>You must have an open Session Scope.</remarks>
		public static QueryOver<T,T> QueryOver(string entityname, Expression<Func<T>> alias) {
			return ActiveRecord<T>.QueryOver(entityname, alias);
		}

		#endregion
	}
}
