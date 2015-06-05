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
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Impl;

namespace Castle.ActiveRecord.Scopes {
    /// <summary>
    /// Contract for implementation of scopes.
    /// </summary>
    /// <remarks>
    /// A scope can implement a logic that affects 
    /// AR for the scope lifetime. Session cache and
    /// transaction are the best examples, but you 
    /// can create new scopes adding new semantics.
    /// </remarks>
    public interface ISessionScope : IDisposable {

        /// <summary>
        /// The holder for the session factories.
        /// </summary>
        ISessionFactoryHolder Holder { get; }

        /// <summary>
        /// Gets or sets the thread scope into to be used by the session scope
        /// </summary>
        IThreadScopeInfo ScopeInfo { get; }

        /// <summary>
        /// Returns the <see cref="FlushAction"/> defined 
        /// for this scope
        /// </summary>
        FlushAction FlushAction { get; }

        /// <summary>
        /// Returns the isolation level defined 
        /// for this scope
        /// </summary>
        IsolationLevel IsolationLevel { get; }

        /// <summary>
        /// Flushes the sessions that this scope 
        /// is maintaining
        /// </summary>
        void Flush();

        /// <summary>
        /// This method will be called if a scope action fails. 
        /// The scope may then decide to use an different approach to flush/dispose it.
        /// </summary>
        void FailScope();

        /// <summary>
        /// Creates a session for the associated type
        /// or returns an existing session if the scope already has one
        /// </summary>
        ISession OpenSession<T>();

        /// <summary>
        /// Finds an object instance by its primary key
        /// returns null if not found
        /// </summary>
        /// <param name="type"></param>
        /// <param name="id">Identifier value</param>
        object Find(Type type, object id);

        /// <summary>
        /// Peeks for an object instance by its primary key,
        /// never returns null
        /// </summary>
        /// <param name="type"></param>
        /// <param name="id">Identifier value</param>
        object Peek(Type type, object id);

        /// <summary>
        /// Finds an object instance by its primary key
        /// returns null if not found
        /// </summary>
        /// <param name="id">Identifier value</param>
        T Find<T>(object id) where T : class;

        /// <summary>
        /// Peeks for an object instance by its primary key,
        /// never returns null
        /// </summary>
        /// <param name="id">Identifier value</param>
        T Peek<T>(object id) where T : class;

        /// <summary>
        /// Check if any instance matches the query.
        /// </summary>
        /// <param name="detachedQuery">The query expression</param>
        /// <returns><c>true</c> if an instance is found; otherwise <c>false</c>.</returns>
        bool Exists<T>(IDetachedQuery detachedQuery) where T : class;

        /// <summary>
        /// Check if any instance matches the criteria.
        /// </summary>
        /// <returns><c>true</c> if an instance is found; otherwise <c>false</c>.</returns>
        bool Exists<T>(params ICriterion[] criterias) where T : class;

        /// <summary>
        /// Check if any instance matching the criteria exists in the database.
        /// </summary>
        /// <param name="expression">The queryover expression</param>
        /// <returns><c>true</c> if an instance is found; otherwise <c>false</c>.</returns>
        bool Exists<T>(Expression<Func<T, bool>> expression) where T : class;

        /// <summary>
        /// Check if any instance matching the criteria exists in the database.
        /// </summary>
        /// <param name="queryover">The queryover expression</param>
        /// <returns><c>true</c> if an instance is found; otherwise <c>false</c>.</returns>
        bool Exists<T>(QueryOver<T, T> queryover) where T : class;

        /// <summary>
        /// Check if any instance matching the criteria exists in the database.
        /// </summary>
        /// <param name="detachedCriteria">The criteria expression</param>
        /// <returns><c>true</c> if an instance is found; otherwise <c>false</c>.</returns>
        bool Exists<T>(DetachedCriteria detachedCriteria) where T : class;

        /// <summary>
        /// Returns the number of records of the specified 
        /// type in the database that match the given critera
        /// </summary>
        /// <param name="criteria">The criteria expression</param>
        /// <returns>The count result</returns>
        int Count<T>(params ICriterion[] criteria) where T : class;

        /// <summary>
        /// Returns the number of records of the specified 
        /// type in the database
        /// </summary>
        /// <param name="expression">The criteria expression</param>
        /// <returns>The count result</returns>
        int Count<T>(Expression<Func<T, bool>> expression) where T : class;

        /// <summary>
        /// Returns the number of records of the specified 
        /// type in the database
        /// </summary>
        /// <param name="queryover">The criteria expression</param>
        /// <returns>The count result</returns>
        int Count<T>(QueryOver<T, T> queryover) where T : class;

        /// <summary>
        /// Returns the number of records of the specified 
        /// type in the database
        /// </summary>
        /// <param name="detachedCriteria">The criteria expression</param>
        /// <returns>The count result</returns>
        int Count<T>(DetachedCriteria detachedCriteria) where T : class;

        /// <summary>
        /// Searches and returns the first row for <typeparamref name="T"/>
        /// </summary>
        /// <param name="order">The sort order - used to determine which record is the first one</param>
        /// <param name="criteria">The criteria expression</param>
        /// <returns>A <c>targetType</c> instance or <c>null</c></returns>
        T FindFirst<T>(Order order, params ICriterion[] criteria) where T : class;

        /// <summary>
        /// Searches and returns the first row.
        /// </summary>
        /// <param name="orders">The sort order - used to determine which record is the first one</param>
        /// <param name="criterias">The criteria expression</param>
        /// <returns>A <c>targetType</c> instance or <c>null</c></returns>
        T FindFirst<T>(Order[] orders, params ICriterion[] criterias) where T : class;

        /// <summary>
        /// Searches and returns the first row.
        /// </summary>
        /// <param name="criterias">The criteria expression</param>
        /// <returns>A <c>targetType</c> instance or <c>null</c></returns>
        T FindFirst<T>(params ICriterion[] criterias) where T : class;

        /// <summary>
        /// Searches and returns the first row.
        /// </summary>
        /// <param name="detachedCriteria">The criteria.</param>
        /// <param name="orders">The sort order - used to determine which record is the first one.</param>
        /// <returns>A <c>targetType</c> instance or <c>null.</c></returns>
        T FindFirst<T>(DetachedCriteria detachedCriteria, params Order[] orders) where T : class;

        /// <summary>
        /// Searches and returns the first row.
        /// </summary>
        /// <param name="detachedQuery">The expression query.</param>
        /// <returns>A <c>targetType</c> instance or <c>null.</c></returns>
        T FindFirst<T>(IDetachedQuery detachedQuery) where T : class;

        /// <summary>
        /// Searches and returns the first row.
        /// </summary>
        /// <param name="criterias">The criterias.</param>
        /// <returns>A instance the targetType or <c>null</c></returns>
        T FindOne<T>(params ICriterion[] criterias) where T : class;

        /// <summary>
        /// Searches and returns a row. If more than one is found, 
        /// throws <see cref="ActiveRecordException"/>
        /// </summary>
        /// <param name="queryover">The QueryOver</param>
        /// <returns>A <c>targetType</c> instance or <c>null</c></returns>
        T FindOne<T>(QueryOver<T, T> queryover) where T : class;

        /// <summary>
        /// Searches and returns a row. If more than one is found, 
        /// throws <see cref="ActiveRecordException"/>
        /// </summary>
        /// <param name="criteria">The criteria</param>
        /// <returns>A <c>targetType</c> instance or <c>null</c></returns>
        T FindOne<T>(DetachedCriteria criteria) where T : class;

        /// <summary>
        /// Searches and returns a row. If more than one is found,
        /// throws <see cref="ActiveRecordException"/>
        /// </summary>
        /// <param name="detachedQuery">The query expression</param>
        /// <returns>A <c>targetType</c> instance or <c>null</c></returns>
        T FindOne<T>(IDetachedQuery detachedQuery) where T : class;

        /// <summary>
        /// Finds records based on a property value - automatically converts null values to IS NULL style queries. 
        /// </summary>
        /// <param name="property">A property name (not a column name)</param>
        /// <param name="value">The value to be equals to</param>
        /// <returns></returns>
        IEnumerable<T> FindAllByProperty<T>(string property, object value) where T : class;

        /// <summary>
        /// Finds records based on a property value - automatically converts null values to IS NULL style queries. 
        /// </summary>
        /// <param name="orderByColumn">The column name to be ordered ASC</param>
        /// <param name="property">A property name (not a column name)</param>
        /// <param name="value">The value to be equals to</param>
        /// <returns></returns>
        IEnumerable<T> FindAllByProperty<T>(string orderByColumn, string property, object value) where T : class;

        /// <summary>
        /// Returns all instances found for the specified type 
        /// using sort orders and criteria.
        /// </summary>
        /// <param name="order">An <see cref="NHibernate.Criterion.Order"/> object.</param>
        /// <param name="criteria">The criteria expression</param>
        /// <returns>The <see cref="Array"/> of results.</returns>
        IEnumerable<T> FindAll<T>(Order order, params ICriterion[] criteria) where T : class;

        /// <summary>
        /// Returns all instances found for the specified type 
        /// using sort orders and criterias.
        /// </summary>
        /// <param name="orders"></param>
        /// <param name="criterias"></param>
        /// <returns></returns>
        IEnumerable<T> FindAll<T>(Order[] orders, params ICriterion[] criterias) where T : class;

        /// <summary>
        /// Returns all instances found for the specified type 
        /// using criterias.
        /// </summary>
        /// <param name="criterias"></param>
        /// <returns></returns>
        IEnumerable<T> FindAll<T>(params ICriterion[] criterias) where T : class;

        /// <summary>
        /// Returns all instances found for the specified type according to the criteria
        /// </summary>
        IEnumerable<T> FindAll<T>(QueryOver<T, T> queryover) where T : class;

        /// <summary>
        /// Returns all instances found for the specified type according to the criteria
        /// </summary>
        IEnumerable<T> FindAll<T>(DetachedCriteria detachedCriteria, params Order[] orders) where T : class;

        /// <summary>
        /// Returns all instances found for the specified type according to the criteria
        /// </summary>
        /// <param name="detachedQuery">The query expression</param>
        /// <returns>The <see cref="Array"/> of results.</returns>
        IEnumerable<T> FindAll<T>(IDetachedQuery detachedQuery) where T : class;

        /// <summary>
        /// Returns a portion of the query results (sliced)
        /// </summary>
        /// <param name="firstResult">The number of the first row to retrieve.</param>
        /// <param name="maxResults">The maximum number of results retrieved.</param>
        /// <param name="order">order</param>
        /// <param name="criteria">criteria</param>
        /// <returns>The sliced query results.</returns>
        IEnumerable<T> SlicedFindAll<T>(int firstResult, int maxResults, Order order, params ICriterion[] criteria) where T : class;

        /// <summary>
        /// Returns a portion of the query results (sliced)
        /// </summary>
        /// <param name="firstResult">The number of the first row to retrieve.</param>
        /// <param name="maxResults">The maximum number of results retrieved.</param>
        /// <param name="orders">An <see cref="Array"/> of <see cref="NHibernate.Criterion.Order"/> objects.</param>
        /// <param name="criteria">The criteria expression</param>
        /// <returns>The sliced query results.</returns>
        IEnumerable<T> SlicedFindAll<T>(int firstResult, int maxResults, Order[] orders, params ICriterion[] criteria) where T : class;

        /// <summary>
        /// Returns a portion of the query results (sliced)
        /// </summary>
        /// <param name="firstResult">The number of the first row to retrieve.</param>
        /// <param name="maxResults">The maximum number of results retrieved.</param>
        /// <param name="criteria">The criteria expression</param>
        /// <returns>The sliced query results.</returns>
        IEnumerable<T> SlicedFindAll<T>(int firstResult, int maxResults, params ICriterion[] criteria) where T : class;

        /// <summary>
        /// Returns a portion of the query results (sliced)
        /// </summary>
        /// <param name="firstResult">The number of the first row to retrieve.</param>
        /// <param name="maxResults">The maximum number of results retrieved.</param>
        /// <param name="orders">An <see cref="Array"/> of <see cref="NHibernate.Criterion.Order"/> objects.</param>
        /// <param name="criteria">The criteria expression</param>
        /// <returns>The sliced query results.</returns>
        IEnumerable<T> SlicedFindAll<T>(int firstResult, int maxResults, DetachedCriteria criteria, params Order[] orders) where T : class;

        /// <summary>
        /// Returns a portion of the query results (sliced)
        /// </summary>
        /// <param name="firstResult">The number of the first row to retrieve.</param>
        /// <param name="maxResults">The maximum number of results retrieved.</param>
        /// <param name="detachedQuery">The query expression</param>
        /// <returns>The sliced query results.</returns>
        IEnumerable<T> SlicedFindAll<T>(int firstResult, int maxResults, IDetachedQuery detachedQuery) where T : class;

        /// <summary>
        /// Returns a portion of the query results (sliced)
        /// </summary>
        /// <param name="firstResult">The number of the first row to retrieve.</param>
        /// <param name="maxResults">The maximum number of results retrieved.</param>
        /// <param name="queryover">Queryover</param>
        /// <returns>The sliced query results.</returns>
        IEnumerable<T> SlicedFindAll<T>(int firstResult, int maxResults, QueryOver<T, T> queryover) where T : class;

        /// <summary>
        /// Deletes all rows for the specified ActiveRecord type that matches
        /// the supplied criteria
        /// </summary>
        void DeleteAll<T>(DetachedCriteria criteria) where T : class;

        /// <summary>
        /// Deletes all rows for the specified ActiveRecord type that matches
        /// the supplied criteria
        /// </summary>
        void DeleteAll<T>(params ICriterion[] criteria) where T : class;

        /// <summary>
        /// Deletes all rows for the specified ActiveRecord type that matches
        /// the supplied expression criteria
        /// </summary>
        void DeleteAll<T>(Expression<Func<T, bool>> expression) where T : class;

        /// <summary>
        /// Deletes all rows for the specified ActiveRecord type that matches
        /// the supplied queryover
        /// </summary>
        void DeleteAll<T>(QueryOver<T, T> queryover) where T : class;

        /// <summary>
        /// Deletes all rows for the specified ActiveRecord type that matches
        /// the supplied HQL condition
        /// </summary>
        /// <param name="where">HQL condition to select the rows to be deleted</param>
        void DeleteAll<T>(string where) where T : class;

        /// <summary>
        /// Deletes all rows for the supplied primary keys 
        /// </summary>
        /// <param name="pkvalues">A list of primary keys</param>
        void DeleteAll<T>(IEnumerable<object> pkvalues) where T : class;

        /// <summary>
        /// Saves the instance to the database. If the primary key is unitialized
        /// it creates the instance on the database. Otherwise it updates it.
        /// <para>
        /// If the primary key is assigned, then you must invoke Create
        /// or Update instead.
        /// </para>
        /// </summary>
        void Save<T>(T instance, bool flush = true) where T : class;

        /// <summary>
        /// Saves a copy of the instance to the database. If the primary key is uninitialized
        /// it creates the instance in the database. Otherwise it updates it.
        /// </summary>
        /// <returns>The saved ActiveRecord instance</returns>
        T SaveCopy<T>(T instance, bool flush = true) where T : class;

        /// <summary>
        /// Creates (Saves) a new instance to the database.
        /// </summary>
        object Create<T>(T instance, bool flush = true) where T : class;

        /// <summary>
        /// Persists the modification on the instance
        /// state to the database.
        /// </summary>
        void Update<T>(T instance, bool flush = true) where T : class;

        /// <summary>
        /// Deletes the instance from the database.
        /// </summary>
        void Delete<T>(T instance, bool flush = true) where T : class;

        /// <summary>
        /// Refresh the instance from the database.
        /// </summary>
        /// <param name="instance">The ActiveRecord instance to be reloaded</param>
        void Refresh<T>(T instance) where T : class;

        /// <summary>
        /// Merge the instance to scope session
        /// </summary>
        /// <param name="instance"></param>
        void Merge<T>(T instance) where T : class;

        /// <summary>
        /// Evict the instance from scope session
        /// </summary>
        /// <param name="instance"></param>
        void Evict<T>(object instance) where T : class;

        /// <summary>
        /// From NHibernate documentation: 
        /// Persist all reachable transient objects, reusing the current identifier 
        /// values. Note that this will not trigger the Interceptor of the Session.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <param name="replicationMode">The replication mode.</param>
        void Replicate<T>(object instance, ReplicationMode replicationMode) where T : class;

        /// <summary>
        /// Provide an IQueryable.
        /// Make sure we are in a scope
        /// </summary>
        IQueryable<T> All<T>() where T : class;

        /// <summary>
        /// The QueryOver method is used as a Linq collection
        /// or as the in argument in a Linq expression. 
        /// </summary>
        /// <remarks>You must have an open Session Scope.</remarks>
        IQueryOver<T, T> QueryOver<T>() where T : class;

        /// <summary>
        /// The QueryOver method is used as a Linq collection
        /// or as the in argument in a Linq expression. 
        /// </summary>
        /// <remarks>You must have an open Session Scope.</remarks>
        IQueryOver<T, T> QueryOver<T>(Expression<Func<T>> alias) where T : class;

        /// <summary>
        /// The QueryOver method is used as a Linq collection
        /// or as the in argument in a Linq expression. 
        /// </summary>
        /// <remarks>You must have an open Session Scope.</remarks>
        IQueryOver<T, T> QueryOver<T>(string entityname) where T : class;

        /// <summary>
        /// The QueryOver method is used as a Linq collection
        /// or as the in argument in a Linq expression. 
        /// </summary>
        /// <remarks>You must have an open Session Scope.</remarks>
        IQueryOver<T, T> QueryOver<T>(string entityname, Expression<Func<T>> alias) where T : class;

        ICriteria CreateCriteria<T>() where T : class;

        IQuery CreateQuery<T>(string hql) where T : class;

        ISQLQuery CreateSqlQuery<T>(string sql) where T : class;

        ICriteria MakeExecutable<T>(DetachedCriteria criteria) where T : class;

        IQuery MakeExecutable<T>(DetachedQuery query) where T : class;

        IQueryOver<T, T> MakeExecutable<T>(QueryOver<T, T> queryover) where T : class;
        /// <summary>
        /// Invokes the specified delegate passing a valid 
        /// NHibernate session. Used for custom NHibernate queries.
        /// </summary>
        /// <returns>Whatever is returned by the delegate invocation</returns>
        TK Execute<T, TK>(Type type, Func<ISession, T, TK> func, T instance);

        /// <summary>
        /// Invokes the specified delegate passing a valid 
        /// NHibernate session. Used for custom NHibernate queries.
        /// </summary>
        void Execute(Type type, Action<ISession> action);

        /// <summary>
        /// Invokes the specified delegate passing a valid 
        /// NHibernate session. Used for custom NHibernate queries.
        /// </summary>
        /// <returns>Whatever is returned by the delegate invocation</returns>
        TK Execute<TK>(Type type, Func<ISession, TK> func);

        /// <summary>
        /// Invokes the specified delegate passing a valid 
        /// NHibernate session. Used for custom NHibernate queries.
        /// </summary>
        /// <returns>Whatever is returned by the delegate invocation</returns>
        TK Execute<T, TK>(Func<ISession, T, TK> func, T instance) where T : class;

        /// <summary>
        /// Invokes the specified delegate passing a valid 
        /// NHibernate session. Used for custom NHibernate queries.
        /// </summary>
        /// <returns>Whatever is returned by the delegate invocation</returns>
        void Execute<T>(Action<ISession> action) where T : class;

        /// <summary>
        /// Invokes the specified delegate passing a valid 
        /// NHibernate session. Used for custom NHibernate queries.
        /// </summary>
        /// <returns>Whatever is returned by the delegate invocation</returns>
        TK Execute<T, TK>(Func<ISession, TK> func) where T : class;

        /// <summary>
        /// This method is invoked when no session was available
        /// at and the current scope just created one. So it
        /// registers the session created within this scope 
        /// using a key. The scope implementation shouldn't make 
        /// any assumption on what the key actually is as we 
        /// reserve the right to change it
        /// <seealso cref="IsKeyKnown"/>
        /// </summary>
        /// <param name="key">an object instance</param>
        /// <param name="session">An instance of <c>ISession</c></param>
        void RegisterSession(object key, ISession session);

        /// <summary>
        /// This method should return the session instance associated with the key.
        /// </summary>
        /// <param name="key">an object instance</param>
        /// <returns>
        /// the session instance or null if none was found
        /// </returns>
        ISession GetSession(object key);

        /// <summary>
        /// Notifies the scope that an inner scope that changed the flush mode, was
        /// disposed. The scope should reset the flush mode to its default.
        /// </summary>
        void ResetFlushMode();

        /// <summary>
        /// This method is invoked when the scope instance needs a session 
        /// instance. Instead of creating one it interrogates
        /// the active scope for one. The scope implementation must check if it
        /// has a session registered for the given key.
        /// <seealso cref="RegisterSession"/>
        /// </summary>
        /// <param name="key">an object instance</param>
        /// <returns>
        ///     <c>true</c> if the key exists within this scope instance
        /// </returns>
        bool IsKeyKnown(object key);

        /// <summary>
        /// Votes to roll back the transaction
        /// </summary>
        void VoteRollBack();

        /// <summary>
        /// Votes to commit the transaction
        /// </summary>
        void VoteCommit();
    }
}
