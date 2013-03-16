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
using Castle.ActiveRecord.Scopes;
using Remotion.Linq.Utilities;

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

        #region Execute

        public static TK Execute<T, TK>(Func<ISession, T, TK> func, T instance) where T : class {
            return AR.CurrentScope().Execute<T, TK>(func, instance);
        }

        /// <summary>
        /// Invokes the specified delegate passing a valid 
        /// NHibernate session. Used for custom NHibernate queries.
        /// </summary>
        /// <param name="action">The delegate instance</param>
        public static void Execute<T>(Action<ISession> action) where T : class {
            AR.CurrentScope().Execute<T>(action);
        }

        /// <summary>
        /// Invokes the specified delegate passing a valid 
        /// NHibernate session. Used for custom NHibernate queries.
        /// </summary>
        /// <param name="func">The delegate instance</param>
        /// <returns>Whatever is returned by the delegate invocation</returns>
        public static TK Execute<T, TK>(Func<ISession, TK> func) where T : class {
            return AR.CurrentScope().Execute<T, TK>(func);
        }

        #endregion

        #region Find/Peek

        /// <summary>
        /// Finds an object instance by its primary key
        /// returns null if not found
        /// </summary>
        /// <param name="type"></param>
        /// <param name="id">Identifier value</param>
        public static object Find(Type type, object id) {
            return AR.CurrentScope().Find(type, id);
        }

        /// <summary>
        /// Peeks for an object instance by its primary key,
        /// never returns null
        /// </summary>
        /// <param name="type"></param>
        /// <param name="id">Identifier value</param>
        public static object Peek(Type type, object id)
        {
            return AR.CurrentScope().Peek(type, id);
        }

        /// <summary>
        /// Finds an object instance by its primary key
        /// returns null if not found
        /// </summary>
        /// <param name="id">Identifier value</param>
        public static T Find<T>(object id) where T : class {
            return AR.CurrentScope().Find<T>(id);
        }

        /// <summary>
        /// Peeks for an object instance by its primary key,
        /// never returns null
        /// </summary>
        /// <param name="id">Identifier value</param>
        public static T Peek<T>(object id) where T : class
        {
            return AR.CurrentScope().Peek<T>(id);
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
            return AR.CurrentScope().Exists<T>(detachedQuery);
        }

        /// <summary>
        /// Check if any instance matches the criteria.
        /// </summary>
        /// <returns><c>true</c> if an instance is found; otherwise <c>false</c>.</returns>
        public static bool Exists<T>(params ICriterion[] criterias) where T : class
        {
            return AR.CurrentScope().Exists<T>(criterias);
        }

        /// <summary>
        /// Check if any instance matching the criteria exists in the database.
        /// </summary>
        /// <param name="expression">The queryover expression</param>
        /// <returns><c>true</c> if an instance is found; otherwise <c>false</c>.</returns>
        public static bool Exists<T>(Expression<Func<T, bool>> expression) where T : class
        {
            return AR.CurrentScope().Exists<T>(expression);
        }

        /// <summary>
        /// Check if any instance matching the criteria exists in the database.
        /// </summary>
        /// <param name="queryover">The queryover expression</param>
        /// <returns><c>true</c> if an instance is found; otherwise <c>false</c>.</returns>
        public static bool Exists<T>(QueryOver<T, T> queryover) where T : class
        {
            return AR.CurrentScope().Exists<T>(queryover);
        }

        /// <summary>
        /// Check if any instance matching the criteria exists in the database.
        /// </summary>
        /// <param name="detachedCriteria">The criteria expression</param>
        /// <returns><c>true</c> if an instance is found; otherwise <c>false</c>.</returns>
        public static bool Exists<T>(DetachedCriteria detachedCriteria) where T : class
        {
            return AR.CurrentScope().Exists<T>(detachedCriteria);
        }

        /// <summary>
        /// Returns the number of records of the specified 
        /// type in the database that match the given critera
        /// </summary>
        /// <param name="criteria">The criteria expression</param>
        /// <returns>The count result</returns>
        public static int Count<T>(params ICriterion[] criteria) where T : class
        {
            return AR.CurrentScope().Count<T>(criteria);
        }

        /// <summary>
        /// Returns the number of records of the specified 
        /// type in the database
        /// </summary>
        /// <param name="expression">The criteria expression</param>
        /// <returns>The count result</returns>
        public static int Count<T>(Expression<Func<T, bool>> expression) where T : class
        {
            return AR.CurrentScope().Count<T>(expression);
        }

        /// <summary>
        /// Returns the number of records of the specified 
        /// type in the database
        /// </summary>
        /// <param name="queryover">The criteria expression</param>
        /// <returns>The count result</returns>
        public static int Count<T>(QueryOver<T, T> queryover) where T : class
        {
            return AR.CurrentScope().Count<T>(queryover);
        }

        /// <summary>
        /// Returns the number of records of the specified 
        /// type in the database
        /// </summary>
        /// <param name="detachedCriteria">The criteria expression</param>
        /// <returns>The count result</returns>
        public static int Count<T>(DetachedCriteria detachedCriteria) where T : class
        {
            return AR.CurrentScope().Count<T>(detachedCriteria);
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
            return AR.CurrentScope().FindFirst<T>(order, criteria);
        }

        /// <summary>
        /// Searches and returns the first row.
        /// </summary>
        /// <param name="orders">The sort order - used to determine which record is the first one</param>
        /// <param name="criterias">The criteria expression</param>
        /// <returns>A <c>targetType</c> instance or <c>null</c></returns>
        public static T FindFirst<T>(Order[] orders, params ICriterion[] criterias) where T : class
        {
            return AR.CurrentScope().FindFirst<T>(orders, criterias);
        }

        /// <summary>
        /// Searches and returns the first row.
        /// </summary>
        /// <param name="criterias">The criteria expression</param>
        /// <returns>A <c>targetType</c> instance or <c>null</c></returns>
        public static T FindFirst<T>(params ICriterion[] criterias) where T : class
        {
            return AR.CurrentScope().FindFirst<T>(criterias);
        }

        /// <summary>
        /// Searches and returns the first row.
        /// </summary>
        /// <param name="detachedCriteria">The criteria.</param>
        /// <param name="orders">The sort order - used to determine which record is the first one.</param>
        /// <returns>A <c>targetType</c> instance or <c>null.</c></returns>
        public static T FindFirst<T>(DetachedCriteria detachedCriteria, params Order[] orders) where T : class
        {
            return AR.CurrentScope().FindFirst<T>(detachedCriteria, orders);
        }

        /// <summary>
        /// Searches and returns the first row.
        /// </summary>
        /// <param name="detachedQuery">The expression query.</param>
        /// <returns>A <c>targetType</c> instance or <c>null.</c></returns>
        public static T FindFirst<T>(IDetachedQuery detachedQuery) where T : class
        {
            return AR.CurrentScope().FindFirst<T>(detachedQuery);
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
            return AR.CurrentScope().FindOne<T>(criterias);
        }

        /// <summary>
        /// Searches and returns a row. If more than one is found, 
        /// throws <see cref="ActiveRecordException"/>
        /// </summary>
        /// <param name="queryover">The QueryOver</param>
        /// <returns>A <c>targetType</c> instance or <c>null</c></returns>
        public static T FindOne<T>(QueryOver<T,T> queryover) where T : class
        {
            return AR.CurrentScope().FindOne(queryover);
        }

        /// <summary>
        /// Searches and returns a row. If more than one is found, 
        /// throws <see cref="ActiveRecordException"/>
        /// </summary>
        /// <param name="criteria">The criteria</param>
        /// <returns>A <c>targetType</c> instance or <c>null</c></returns>
        public static T FindOne<T>(DetachedCriteria criteria) where T : class
        {
            return AR.CurrentScope().FindOne<T>(criteria);
        }

        /// <summary>
        /// Searches and returns a row. If more than one is found,
        /// throws <see cref="ActiveRecordException"/>
        /// </summary>
        /// <param name="detachedQuery">The query expression</param>
        /// <returns>A <c>targetType</c> instance or <c>null</c></returns>
        public static T FindOne<T>(IDetachedQuery detachedQuery) where T : class
        {
            return AR.CurrentScope().FindOne<T>(detachedQuery);
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
            return AR.CurrentScope().FindAllByProperty<T>(property, value);
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
            return AR.CurrentScope().FindAllByProperty<T>(orderByColumn, property, value);
        }

        #endregion

        #region FindAll

        /// <summary>
        /// Returns all instances found for the specified type 
        /// using sort orders and criteria.
        /// </summary>
        /// <param name="order">An <see cref="NHibernate.Criterion.Order"/> object.</param>
        /// <param name="criteria">The criteria expression</param>
        /// <returns>The <see cref="Array"/> of results.</returns>
        public static IEnumerable<T> FindAll<T>(Order order, params ICriterion[] criteria) where T : class
        {
            return AR.CurrentScope().FindAll<T>(order, criteria);
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
            return AR.CurrentScope().FindAll<T>(orders, criterias);
        }

        /// <summary>
        /// Returns all instances found for the specified type 
        /// using criterias.
        /// </summary>
        /// <param name="criterias"></param>
        /// <returns></returns>
        public static IEnumerable<T> FindAll<T>(params ICriterion[] criterias) where T : class
        {
            return AR.CurrentScope().FindAll<T>(criterias);
        }

        /// <summary>
        /// Returns all instances found for the specified type according to the criteria
        /// </summary>
        public static IEnumerable<T> FindAll<T>(QueryOver<T, T> queryover) where T : class
        {
            return AR.CurrentScope().FindAll<T>(queryover);
        }

        /// <summary>
        /// Returns all instances found for the specified type according to the criteria
        /// </summary>
        public static IEnumerable<T> FindAll<T>(DetachedCriteria detachedCriteria, params Order[] orders) where T : class
        {
            return AR.CurrentScope().FindAll<T>(detachedCriteria, orders);
        }

        /// <summary>
        /// Returns all instances found for the specified type according to the criteria
        /// </summary>
        /// <param name="detachedQuery">The query expression</param>
        /// <returns>The <see cref="Array"/> of results.</returns>
        public static IEnumerable<T> FindAll<T>(IDetachedQuery detachedQuery) where T : class
        {
            return AR.CurrentScope().FindAll<T>(detachedQuery);
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
            return AR.CurrentScope().SlicedFindAll<T>(firstResult, maxResults, order, criteria);
        }

        /// <summary>
        /// Returns a portion of the query results (sliced)
        /// </summary>
        /// <param name="firstResult">The number of the first row to retrieve.</param>
        /// <param name="maxResults">The maximum number of results retrieved.</param>
        /// <param name="orders">An <see cref="Array"/> of <see cref="NHibernate.Criterion.Order"/> objects.</param>
        /// <param name="criteria">The criteria expression</param>
        /// <returns>The sliced query results.</returns>
        public static IEnumerable<T> SlicedFindAll<T>(int firstResult, int maxResults, Order[] orders, params ICriterion[] criteria) where T : class
        {
            return AR.CurrentScope().SlicedFindAll<T>(firstResult, maxResults, orders, criteria);
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
            return AR.CurrentScope().SlicedFindAll<T>(firstResult, maxResults, criteria);
        }

        /// <summary>
        /// Returns a portion of the query results (sliced)
        /// </summary>
        /// <param name="firstResult">The number of the first row to retrieve.</param>
        /// <param name="maxResults">The maximum number of results retrieved.</param>
        /// <param name="orders">An <see cref="Array"/> of <see cref="NHibernate.Criterion.Order"/> objects.</param>
        /// <param name="criteria">The criteria expression</param>
        /// <returns>The sliced query results.</returns>
        public static IEnumerable<T> SlicedFindAll<T>(int firstResult, int maxResults, DetachedCriteria criteria, params Order[] orders) where T : class
        {
            return AR.CurrentScope().SlicedFindAll<T>(firstResult, maxResults, criteria, orders);
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
            return AR.CurrentScope().SlicedFindAll<T>(firstResult, maxResults, detachedQuery);
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
            return AR.CurrentScope().SlicedFindAll<T>(firstResult, maxResults, queryover);
        }

        #endregion

        #region DeleteAll

        /// <summary>
        /// Deletes all rows for the specified ActiveRecord type that matches
        /// the supplied criteria
        /// </summary>
        public static void DeleteAll<T>(DetachedCriteria criteria) where T : class
        {
            AR.CurrentScope().DeleteAll<T>(criteria);
        }

        /// <summary>
        /// Deletes all rows for the specified ActiveRecord type that matches
        /// the supplied criteria
        /// </summary>
        public static void DeleteAll<T>(params ICriterion[] criteria) where T : class
        {
            AR.CurrentScope().DeleteAll<T>(criteria);
        }

        /// <summary>
        /// Deletes all rows for the specified ActiveRecord type that matches
        /// the supplied expression criteria
        /// </summary>
        public static void DeleteAll<T>(Expression<Func<T, bool>> expression) where T : class
        {
            AR.CurrentScope().DeleteAll(expression);
        }

        /// <summary>
        /// Deletes all rows for the specified ActiveRecord type that matches
        /// the supplied queryover
        /// </summary>
        public static void DeleteAll<T>(QueryOver<T, T> queryover) where T : class
        {
            AR.CurrentScope().DeleteAll(queryover);
        }

        /// <summary>
        /// Deletes all rows for the specified ActiveRecord type that matches
        /// the supplied HQL condition
        /// </summary>
        /// <param name="where">HQL condition to select the rows to be deleted</param>
        public static void DeleteAll<T>(string where) where T : class
        {
            AR.CurrentScope().DeleteAll<T>(where);
        }

        /// <summary>
        /// Deletes all rows for the supplied primary keys 
        /// </summary>
        /// <param name="pkvalues">A list of primary keys</param>
        public static void DeleteAll<T>(IEnumerable<object> pkvalues) where T : class
        {
            AR.CurrentScope().DeleteAll<T>(pkvalues);
        }

        #endregion

        #region Save/SaveCopy/Create/Update/Delete

        /// <summary>
        /// Saves the instance to the database and flushes the session. If the primary key is unitialized
        /// it creates the instance on the database. Otherwise it updates it.
        /// <para>
        /// If the primary key is assigned, then you must invoke Create
        /// or Update instead.
        /// </para>
        /// </summary>
        public static void Save<T>(T instance, bool flush = true) where T : class
        {
            AR.CurrentScope().Save(instance, flush);
        }

        /// <summary>
        /// Saves a copy of the instance to the database. If the primary key is uninitialized
        /// it creates the instance in the database. Otherwise it updates it.
        /// </summary>
        /// <returns>The saved ActiveRecord instance</returns>
        public static T SaveCopy<T>(T instance, bool flush = true) where T : class
        {
            return AR.CurrentScope().SaveCopy(instance, flush);
        }

        /// <summary>
        /// Creates (Saves) a new instance to the database.
        /// </summary>
        public static void Create<T>(T instance, bool flush = true) where T : class
        {
            AR.CurrentScope().Create(instance, flush);
        }

        /// <summary>
        /// Persists the modification on the instance
        /// state to the database and flushes the session.
        /// </summary>
        public static void Update<T>(T instance, bool flush = true) where T : class
        {
            AR.CurrentScope().Update(instance, flush);
        }

        /// <summary>
        /// Deletes the instance from the database and flushes the session.
        /// </summary>
        public static void Delete<T>(T instance, bool flush = true) where T : class
        {
            AR.CurrentScope().Delete(instance, flush);
        }

        #endregion

        #region Refresh/Merge/Evict/Replicate

        /// <summary>
        /// Refresh the instance from the database.
        /// </summary>
        /// <param name="instance">The ActiveRecord instance to be reloaded</param>
        public static void Refresh<T>(T instance) where T : class
        {
            AR.CurrentScope().Refresh(instance);
        }

        /// <summary>
        /// Merge the instance to scope session
        /// </summary>
        /// <param name="instance"></param>
        public static void Merge<T>(T instance) where T : class
        {
            AR.CurrentScope().Merge(instance);
        }

        /// <summary>
        /// Evict the instance from scope session
        /// </summary>
        /// <param name="instance"></param>
        public static void Evict<T>(T instance) where T : class
        {
            AR.CurrentScope().Evict<T>(instance);
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
            AR.CurrentScope().Replicate<T>(instance, replicationMode);
        }

        #endregion

        #region Linq/QueryOver

        /// <summary>
        /// Provide an IQueryable.
        /// Make sure we are in a scope
        /// </summary>
        public static IQueryable<T> All<T>() where T : class
        {
            return AR.CurrentScope().All<T>();
        }


        /// <summary>
        /// The QueryOver method is used as a Linq collection
        /// or as the in argument in a Linq expression. 
        /// </summary>
        /// <remarks>You must have an open Session Scope.</remarks>
        public static IQueryOver<T> QueryOver<T>() where T : class
        {
            return AR.CurrentScope().QueryOver<T>();
        }

        /// <summary>
        /// The QueryOver method is used as a Linq collection
        /// or as the in argument in a Linq expression. 
        /// </summary>
        /// <remarks>You must have an open Session Scope.</remarks>
        public static IQueryOver<T> QueryOver<T>(Expression<Func<T>> alias) where T : class
        {
            return AR.CurrentScope().QueryOver(alias);
        }

        /// <summary>
        /// The QueryOver method is used as a Linq collection
        /// or as the in argument in a Linq expression. 
        /// </summary>
        /// <remarks>You must have an open Session Scope.</remarks>
        public static IQueryOver<T> QueryOver<T>(string entityname) where T : class
        {
            return AR.CurrentScope().QueryOver<T>(entityname);
        }

        /// <summary>
        /// The QueryOver method is used as a Linq collection
        /// or as the in argument in a Linq expression. 
        /// </summary>
        /// <remarks>You must have an open Session Scope.</remarks>
        public static IQueryOver<T> QueryOver<T>(string entityname, Expression<Func<T>> alias) where T : class
        {
            return AR.CurrentScope().QueryOver(entityname, alias);
        }

        #endregion
    }
}
