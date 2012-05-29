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

namespace Castle.ActiveRecord
{
	using System;
	using System.Collections;
	using Castle.ActiveRecord.Framework;
	using NHibernate;
	using NHibernate.Criterion;

	/// <summary>
	/// Base class for all ActiveRecord Generic classes. 
	/// Implements all the functionality to simplify the code on the subclasses.
	/// </summary>
	[Serializable]
	public abstract class ActiveRecordBase<T> : ActiveRecordHooksBase where T : class
	{
		#region DeleteAll

		/// <summary>
		/// Deletes all rows for the specified ActiveRecord type
		/// </summary>
		/// <remarks>
		/// This method is usually useful for test cases.
		/// </remarks>
		public static void DeleteAll()
		{
			ActiveRecordMediator<T>.DeleteAll();
		}

		/// <summary>
		/// Deletes all rows for the specified ActiveRecord type that matches
		/// the supplied HQL condition
		/// </summary>
		/// <remarks>
		/// This method is usually useful for test cases.
		/// </remarks>
		/// <param name="where">HQL condition to select the rows to be deleted</param>
		public static void DeleteAll(String where)
		{
			ActiveRecordMediator<T>.DeleteAll(where);
		}

		/// <summary>
		/// Deletes all <typeparamref name="T"/> objects, based on the primary keys
		/// supplied on <paramref name="pkValues" />.
		/// </summary>
		/// <returns>The number of objects deleted</returns>
		public static int DeleteAll(IEnumerable pkValues)
		{
			return ActiveRecordMediator<T>.DeleteAll(pkValues);
		}

		#endregion

		#region Exists

		/// <summary>
		/// Check if the <paramref name="id"/> exists in the database.
		/// </summary>
		/// <param name="id">The id to check on</param>
		/// <returns><c>true</c> if the ID exists; otherwise <c>false</c>.</returns>
		public static bool Exists(object id)
		{
			return ActiveRecordMediator<T>.Exists(id);
		}

		/// <summary>
		/// Check if any instance matching the criteria exists in the database.
		/// </summary>
		/// <param name="criteria">The criteria expression</param>		
		/// <returns><c>true</c> if an instance is found; otherwise <c>false</c>.</returns>
		public static bool Exists(params ICriterion[] criteria)
		{
			return ActiveRecordMediator<T>.Exists(criteria);
		}

		/// <summary>
		/// Check if any instance matching the criteria exists in the database.
		/// </summary>
		/// <param name="detachedCriteria">The criteria expression</param>		
		/// <returns><c>true</c> if an instance is found; otherwise <c>false</c>.</returns>
		public static bool Exists(DetachedCriteria detachedCriteria)
		{
			return ActiveRecordMediator<T>.Exists(detachedCriteria);
		}

		/// <summary>
		/// Check if any instance matching the query exists in the database.
		/// </summary>
		/// <param name="detachedQuery">The query expression</param>
		/// <returns>true if an instance is found; otherwise false.</returns>
		public static bool Exists(IDetachedQuery detachedQuery)
		{
			return ActiveRecordMediator<T>.Exists(detachedQuery);
		}

		#endregion

		#region FindAll

		/// <summary>
		/// Returns all the instances that match the detached criteria.
		/// </summary>
		/// <param name="criteria">Detached criteria</param>
		/// <param name="orders">Optional ordering</param>
		/// <returns>All entities that match the criteria</returns>
		public static IEnumerable<T> FindAll(DetachedCriteria criteria, params Order[] orders)
		{
			return ActiveRecordMediator<T>.FindAll(criteria, orders);
		}

		/// <summary>
		/// Returns all instances found for the specified type 
		/// using sort orders and criteria.
		/// </summary>
		/// <param name="order">An <see cref="Order"/> object.</param>
		/// <param name="criteria">The criteria expression</param>
		/// <returns>The <see cref="Array"/> of results.</returns>
		public static IEnumerable<T> FindAll(Order order, params ICriterion[] criteria)
		{
			return ActiveRecordMediator<T>.FindAll(new[] {order}, criteria);
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
			return ActiveRecordMediator<T>.FindAll(orders, criteria);
		}

		/// <summary>
		/// Returns all instances found for <typeparamref name="T"/>
		/// using criteria.
		/// </summary>
		/// <param name="criteria"></param>
		/// <returns>An <see cref="Array"/> of <typeparamref name="T"/></returns>
		public static IEnumerable<T> FindAll(params ICriterion[] criteria)
		{
			return ActiveRecordMediator<T>.FindAll(criteria);
		}

		/// <summary>
		/// Returns all instances found for the specified type according to the criteria
		/// </summary>
		/// <param name="detachedQuery">The query expression.</param>
		/// <returns>All entities that match the query</returns>
		public static IEnumerable<T> FindAll(IDetachedQuery detachedQuery)
		{
			return ActiveRecordMediator<T>.FindAll(detachedQuery);
		}


		#endregion

		#region FindAllByProperty

		/// <summary>
		/// Finds records based on a property value
		/// </summary>
		/// <param name="property">A property name (not a column name)</param>
		/// <param name="value">The value to be equals to</param>
		/// <returns>An <see cref="Array"/> of <typeparamref name="T"/></returns>
		public static IEnumerable<T> FindAllByProperty(String property, object value)
		{
			return ActiveRecordMediator<T>.FindAllByProperty(property, value);
		}

		/// <summary>
		/// Finds records based on a property value
		/// </summary>
		/// <param name="orderByColumn">The column name to be ordered ASC</param>
		/// <param name="property">A property name (not a column name)</param>
		/// <param name="value">The value to be equals to</param>
		/// <returns>An <see cref="Array"/> of <typeparamref name="T"/></returns>
		public static IEnumerable<T> FindAllByProperty(String orderByColumn, String property, object value)
		{
			return ActiveRecordMediator<T>.FindAllByProperty(orderByColumn, property, value);
		}

		#endregion

		#region Find/TryFind

		/// <summary>
		/// Finds an object instance by an unique ID 
		/// </summary>
		/// <param name="id">ID value</param>
		/// <exception cref="ObjectNotFoundException">if the row is not found</exception>
		/// <returns>T</returns>
		public static T Find(object id)
		{
			return ActiveRecordMediator<T>.FindByPrimaryKey(id, true);
		}

		/// <summary>
		/// Finds an object instance by an unique ID.
		/// If the row is not found this method will not throw an exception.
		/// </summary>
		/// <param name="id">ID value</param>
		/// <returns>A <typeparamref name="T"/></returns>
		public static T TryFind(object id)
		{
			return ActiveRecordMediator<T>.FindByPrimaryKey(id, false);
		}

		#endregion

		#region FindFirst

		/// <summary>
		/// Searches and returns the first row for <typeparamref name="T"/>.
		/// </summary>
		/// <param name="criteria">Detached criteria.</param>
		/// <param name="orders">The sort order - used to determine which record is the first one.</param>
		/// <returns>A <c>targetType</c> instance or <c>null</c>.</returns>
		public static T FindFirst(DetachedCriteria criteria, params Order[] orders)
		{
			return ActiveRecordMediator<T>.FindFirst(criteria, orders);
		}

		/// <summary>
		/// Searches and returns the first row for <typeparamref name="T"/>
		/// </summary>
		/// <param name="order">The sort order - used to determine which record is the first one</param>
		/// <param name="criteria">The criteria expression</param>
		/// <returns>A <c>targetType</c> instance or <c>null</c></returns>
		public static T FindFirst(Order order, params ICriterion[] criteria)
		{
			return ActiveRecordMediator<T>.FindFirst(new Order[] {order}, criteria);
		}

		/// <summary>
		/// Searches and returns the first row for <typeparamref name="T"/>
		/// </summary>
		/// <param name="orders">The sort order - used to determine which record is the first one</param>
		/// <param name="criteria">The criteria expression</param>
		/// <returns>A <c>targetType</c> instance or <c>null</c></returns>
		public static T FindFirst(Order[] orders, params ICriterion[] criteria)
		{
			return ActiveRecordMediator<T>.FindFirst(orders, criteria);
		}

		/// <summary>
		/// Searches and returns the first row for <typeparamref name="T"/>
		/// </summary>
		/// <param name="criteria">The criteria expression</param>
		/// <returns>A <c>targetType</c> instance or <c>null</c></returns>
		public static T FindFirst(params ICriterion[] criteria)
		{
			return ActiveRecordMediator<T>.FindFirst(criteria);
		}

		/// <summary>
		/// Searches and returns the first row. 
		/// </summary>
		/// <param name="detachedQuery">The query expression</param>
		/// <returns>A <c>targetType</c> instance or <c>null.</c></returns>
		public static T FindFirst(IDetachedQuery detachedQuery) 
		{
			return ActiveRecordMediator<T>.FindFirst(detachedQuery);
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
			return ActiveRecordMediator<T>.FindOne(criteria);
		}

		/// <summary>
		/// Searches and returns a row. If more than one is found, 
		/// throws <see cref="ActiveRecordException"/>
		/// </summary>
		/// <param name="criteria">The criteria</param>
		/// <returns>A <c>targetType</c> instance or <c>null</c></returns>
		public static T FindOne(DetachedCriteria criteria)
		{
			return ActiveRecordMediator<T>.FindOne(criteria);
		}

		/// <summary>
		/// Searches and returns a row. If more than one is found, 
		/// throws <see cref="ActiveRecordException"/>
		/// <param name="detachedQuery">The query expression</param>
		/// </summary>
		/// <returns>A <c>targetType</c> instance or <c>null</c></returns>
		public static T FindOne(IDetachedQuery detachedQuery) 
		{
			return ActiveRecordMediator<T>.FindOne(detachedQuery);
		}

		#endregion

		#region SlicedFindAll

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
			return ActiveRecordMediator<T>.SlicedFindAll(firstResult, maxResults, orders, criteria);
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
			return ActiveRecordMediator<T>.SlicedFindAll(firstResult, maxResults, criteria);
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
			return ActiveRecordMediator<T>.SlicedFindAll(firstResult, maxResults, criteria, orders);
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
			return ActiveRecordMediator<T>.SlicedFindAll(firstResult, maxResults, detachedQuery);
		}

		#endregion

		/// <summary>
		/// The QueryOver method is used as a Linq collection
		/// or as the in argument in a Linq expression. 
		/// </summary>
		/// <remarks>You must have an open Session Scope.</remarks>
		public static QueryOver<T,T> QueryOver() {
			return NHibernate.Criterion.QueryOver.Of<T>();
		}

		/// <summary>
		/// The QueryOver method is used as a Linq collection
		/// or as the in argument in a Linq expression. 
		/// </summary>
		/// <remarks>You must have an open Session Scope.</remarks>
		public static QueryOver<T,T> QueryOver(Expression<Func<T>> alias) {
			return NHibernate.Criterion.QueryOver.Of(alias);
		}

		/// <summary>
		/// The QueryOver method is used as a Linq collection
		/// or as the in argument in a Linq expression. 
		/// </summary>
		/// <remarks>You must have an open Session Scope.</remarks>
		public static QueryOver<T,T> QueryOver(string entityname) {
			return NHibernate.Criterion.QueryOver.Of<T>(entityname);
		}

		/// <summary>
		/// The QueryOver method is used as a Linq collection
		/// or as the in argument in a Linq expression. 
		/// </summary>
		/// <remarks>You must have an open Session Scope.</remarks>
		public static QueryOver<T,T> QueryOver(string entityname, Expression<Func<T>> alias) {
			return NHibernate.Criterion.QueryOver.Of(entityname, alias);
		}

		public virtual void Create() {
			ActiveRecordMediator<T>.Create(this);
		}

		public virtual void CreateAndFlush() {
			ActiveRecordMediator<T>.CreateAndFlush(this);
		}

		public virtual void Update() {
			ActiveRecordMediator<T>.Update(this);
		}

		public virtual void UpdateAndFlush() {
			ActiveRecordMediator<T>.UpdateAndFlush(this);
		}

		public virtual void Save() {
			ActiveRecordMediator<T>.Save(this);
		}

		public virtual void SaveAndFlush() {
			ActiveRecordMediator<T>.SaveAndFlush(this);
		}

		public virtual void Delete() {
			ActiveRecordMediator<T>.Delete(this);
		}

		public virtual void DeleteAndFlush() {
			ActiveRecordMediator<T>.DeleteAndFlush(this);
		}

		public virtual void Refresh() {
			ActiveRecordMediator<T>.Refresh(this);
		}

		public static IEnumerable<T> FindByProperty(string property, object value) {
			return ActiveRecordMediator<T>.FindAllByProperty(property, value);
		}
	}
}
