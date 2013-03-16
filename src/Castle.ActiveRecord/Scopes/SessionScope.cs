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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Castle.ActiveRecord.Config;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Impl;
using NHibernate.Linq;
using NHibernate.Transform;
using Remotion.Linq.Utilities;

namespace Castle.ActiveRecord.Scopes
{
    /// <summary>
    /// Implementation of <see cref="ISessionScope"/> to 
    /// augment performance by caching the session, thus
    /// avoiding too much opens/flushes/closes.
    /// </summary>
    public class SessionScope : MarshalByRefObject, ISessionScope
    {
        public ISessionFactoryHolder Holder { get; protected set; }
        public IThreadScopeInfo ScopeInfo { get; protected set; }

        /// <summary>
        /// Map between a key to its session
        /// </summary>
        protected readonly IDictionary<object, ISession> Key2Session = new Dictionary<object, ISession>();

        protected ISessionScope ParentScope = null;
        protected bool Rollback, SetForCommit;
        /// <summary>
        /// Initializes a new instance of the <see cref="SessionScope"/> class.
        /// </summary>
        public SessionScope(
            FlushAction flushAction = FlushAction.Config,
            IsolationLevel isolation = IsolationLevel.Unspecified,
            OnDispose ondispose = OnDispose.Commit,
            ISessionScope parent = null,
            ISessionFactoryHolder holder = null,
            IThreadScopeInfo scopeinfo = null
            ) {

            FlushAction = flushAction;
            IsolationLevel = isolation;
            HasSessionError = false;
            OnDisposeBehavior = ondispose;

            Holder = holder ?? AR.Holder;

            ScopeInfo = scopeinfo ?? AR.Holder.ThreadScopeInfo;

            if (parent != null)
                ParentScope = parent;
            else
                if (ScopeInfo.HasInitializedScope)
                    ParentScope = ScopeInfo.GetRegisteredScope();

            ScopeInfo.RegisterScope(this);
        }

        /// <summary>
        /// Returns the <see cref="ISessionScope.FlushAction"/> defined 
        /// for this scope
        /// </summary>
        public FlushAction FlushAction { get; protected set; }

        public IsolationLevel IsolationLevel { get; protected set; }

        /// <summary>
        /// Flushes the sessions that this scope 
        /// is maintaining
        /// </summary>
        public virtual void Flush() {
            if (ParentScope != null) {
                ParentScope.Flush();
            }
            Key2Session.Values.ForEach(s => s.Flush());
        }

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
        public virtual bool IsKeyKnown(object key) {
            return ParentScope != null ? ParentScope.IsKeyKnown(key) : Key2Session.ContainsKey(key);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public virtual void Dispose() {
            ScopeInfo.UnRegisterScope(this);

            PerformDisposal(ParentScope == null);
#if DEBUG
            System.Diagnostics.Debug.Assert(Key2Session.Count == 0);
#endif
        }

        protected readonly OnDispose OnDisposeBehavior;

        /// <summary>
        /// Votes to roll back the transaction
        /// </summary>
        public virtual void VoteRollBack() {
            if (ParentScope != null)
                ParentScope.VoteRollBack();
            Rollback = true;
        }

        /// <summary>
        /// Votes to commit the transaction
        /// </summary>
        public virtual void VoteCommit() {
            if (Rollback) {
                throw new NHibernate.TransactionException("The transaction was marked as rollback " +
                                                          "only - by itself or one of the nested transactions");
            }
            if (ParentScope != null) {
                ParentScope.VoteCommit();
            }
            SetForCommit = true;
        }

        /// <summary>
        /// Gets or sets a flag indicating whether this instance has session error.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this instance has session error; otherwise, <c>false</c>.
        /// </value>
        public bool HasSessionError { get; private set; }

        /// <summary>
        /// This method will be called if a scope action fails. 
        /// The scope may then decide to use an different approach to flush/dispose it.
        /// </summary>
        public virtual void FailScope() {
            VoteRollBack();
        }

        /// <summary>
        /// Sets the flush mode.
        /// </summary>
        /// <param name="session">The session.</param>
        protected virtual void SetFlushMode(ISession session) {
            switch (FlushAction) {
                case FlushAction.Auto:
                    session.FlushMode = FlushMode.Auto;
                    break;
                case FlushAction.Never:
                    session.FlushMode = FlushMode.Never;
                    break;
                default:
                    switch (Holder.ConfigurationSource.DefaultFlushType) {
                            case DefaultFlushType.Auto:
                                session.FlushMode = FlushMode.Auto;
                                break;
                            case DefaultFlushType.Leave:
                            case DefaultFlushType.Transaction:
                                session.FlushMode = FlushMode.Commit;
                                break;
                            default:
                                session.FlushMode = FlushMode.Auto;
                                break;
                    }
                    break;
            }
        }

        /// <summary>
        /// Notifies the scope that an inner scope that changed the flush mode, was
        /// disposed. The scope should reset the flush mode to its default.
        /// </summary>
        public virtual void ResetFlushMode() {
            Key2Session.Values.ForEach(SetFlushMode);
        }

        /// <summary>
        /// Creates a session for the associated type
        /// </summary>
        public virtual ISession OpenSession<T>() {
            return OpenSession(typeof(T));
        }

        protected virtual ISession OpenSession(Type type) {
            var sessionFactory = Holder.GetSessionFactory(type);
            lock(sessionFactory)
            {
#if DEBUG
                System.Diagnostics.Debug.Assert(sessionFactory != null);
#endif
                if (IsKeyKnown(sessionFactory))
                {
                    return GetSession(sessionFactory);
                }

                var session = CreateSession(sessionFactory, InterceptorFactory.Create());
#if DEBUG
                System.Diagnostics.Debug.Assert(session != null);
#endif
                RegisterSession(sessionFactory, session);
                Initialize(session);

                return session;
            }
        }

        /// <summary>
        /// This method is invoked when no session was available
        /// at and the <see cref="ISessionFactoryHolder"/>
        /// just created one. So it registers the session created
        /// within this scope using a key. The scope implementation
        /// shouldn't make any assumption on what the key
        /// actually is as we reserve the right to change it
        /// <seealso cref="IsKeyKnown"/>
        /// </summary>
        /// <param name="key">an object instance</param>
        /// <param name="session">An instance of <c>ISession</c></param>
        public virtual void RegisterSession(object key, ISession session) {
            if (ParentScope != null) {
                ParentScope.RegisterSession(key, session);
                return;
            }
            Key2Session.Add(key, session);
        }

        /// <summary>
        /// This method should return the session instance associated with the key.
        /// </summary>
        /// <param name="key">an object instance</param>
        /// <returns>
        /// the session instance or null if none was found
        /// </returns>
        public virtual ISession GetSession(object key) {
            if (ParentScope != null) {
                return ParentScope.GetSession(key);
            }
            return Key2Session[key];
        }

        /// <summary>
        /// This method is invoked to allow the scope to create a properly configured session
        /// </summary>
        /// <param name="sessionFactory">From where to open the session</param>
        /// <param name="interceptor">the NHibernate interceptor</param>
        /// <returns>the newly created session</returns>
        protected virtual ISession CreateSession(ISessionFactory sessionFactory, IInterceptor interceptor)
        {
            return sessionFactory.OpenSession(interceptor);
        }

        /// <summary>
        /// Initializes the specified session.
        /// </summary>
        /// <param name="session">The session.</param>
        protected virtual void Initialize(ISession session) {
            SetFlushMode(session);
            session.BeginTransaction(IsolationLevel);
        }
        /// <summary>
        /// Performs the disposal.
        /// </summary>
        protected internal void PerformDisposal(bool dispose = true, bool commit = true) {
            if (!SetForCommit && !Rollback) {
                // Neither VoteCommit or VoteRollback were called
                // and roll back on dispose
                if (OnDisposeBehavior == OnDispose.Rollback)
                    VoteRollBack();
            }

            var exceptions = new List<Exception>();
            foreach (var key in Key2Session.Keys.ToArray()) {
                var session = Key2Session[key];
                try {
                    if ((Rollback || !HasSessionError) && FlushAction != FlushAction.Never) {
                        session.Flush();
                    }
                } catch (Exception e) {
                    exceptions.Add(e);
                }
            }
            HasSessionError = HasSessionError && exceptions.Count > 0;

            if (commit) {
                foreach (var key in Key2Session.Keys.ToArray()) {
                    var s = Key2Session[key];
                    CommitOrRollback(s);
                    if (dispose)
                        s.Dispose();
                    Key2Session.Remove(key);
                }
            }
        }

        private void CommitOrRollback(ISession s) {
            var tx = s.Transaction;
            if (!s.IsConnected || s.Connection.State != ConnectionState.Open || 
                tx == null || !tx.IsActive || (tx.WasCommitted || tx.WasRolledBack)) {
                return;
            }

            if (!Rollback && !HasSessionError) {
                tx.Commit();
            } else {
                tx.Rollback();
            }
            tx.Dispose();
        }

        #region Find/Peek

        /// <summary>
        /// Finds an object instance by its primary key
        /// returns null if not found
        /// </summary>
        /// <param name="type"></param>
        /// <param name="id">Identifier value</param>
        public object Find(Type type, object id)
        {
            return id == null ? null :
                Execute(type, session => session.Get(type, ConvertId(type, id)));
        }

        /// <summary>
        /// Peeks for an object instance by its primary key,
        /// never returns null
        /// </summary>
        /// <param name="type"></param>
        /// <param name="id">Identifier value</param>
        public  object Peek(Type type, object id) {
            return Execute(type, session => session.Load(type, ConvertId(type, id)));
        }

        /// <summary>
        /// Finds an object instance by its primary key
        /// returns null if not found
        /// </summary>
        /// <param name="id">Identifier value</param>
        public T Find<T>(object id) where T : class
        {
            return id == null ? null : Execute<T, T>(session => session.Get<T>(ConvertId<T>(id)));
        }

        /// <summary>
        /// Peeks for an object instance by its primary key,
        /// never returns null
        /// </summary>
        /// <param name="id">Identifier value</param>
        public T Peek<T>(object id) where T : class
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
        public bool Exists<T>(IDetachedQuery detachedQuery) where T : class
        {
            return SlicedFindAll<T>(0, 1, detachedQuery).Any();
        }

        /// <summary>
        /// Check if any instance matches the criteria.
        /// </summary>
        /// <returns><c>true</c> if an instance is found; otherwise <c>false</c>.</returns>
        public bool Exists<T>(params ICriterion[] criterias) where T : class
        {
            return Count<T>(criterias) > 0;
        }

        /// <summary>
        /// Check if any instance matching the criteria exists in the database.
        /// </summary>
        /// <param name="expression">The queryover expression</param>
        /// <returns><c>true</c> if an instance is found; otherwise <c>false</c>.</returns>
        public bool Exists<T>(Expression<Func<T, bool>> expression) where T : class
        {
            return Count<T>(expression) > 0;
        }

        /// <summary>
        /// Check if any instance matching the criteria exists in the database.
        /// </summary>
        /// <param name="queryover">The queryover expression</param>
        /// <returns><c>true</c> if an instance is found; otherwise <c>false</c>.</returns>
        public bool Exists<T>(QueryOver<T, T> queryover) where T : class
        {
            return Count<T>(queryover) > 0;
        }

        /// <summary>
        /// Check if any instance matching the criteria exists in the database.
        /// </summary>
        /// <param name="detachedCriteria">The criteria expression</param>
        /// <returns><c>true</c> if an instance is found; otherwise <c>false</c>.</returns>
        public bool Exists<T>(DetachedCriteria detachedCriteria) where T : class
        {
            return Count<T>(detachedCriteria) > 0;
        }

        /// <summary>
        /// Returns the number of records of the specified 
        /// type in the database that match the given critera
        /// </summary>
        /// <param name="criteria">The criteria expression</param>
        /// <returns>The count result</returns>
        public int Count<T>(params ICriterion[] criteria) where T : class
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
        public int Count<T>(Expression<Func<T, bool>> expression) where T : class {
            return Execute<T, int>(session => session.QueryOver<T>().Where(expression).RowCount());
        }

        /// <summary>
        /// Returns the number of records of the specified 
        /// type in the database
        /// </summary>
        /// <param name="queryover">The criteria expression</param>
        /// <returns>The count result</returns>
        public int Count<T>(QueryOver<T, T> queryover) where T : class
        {
            return Execute<T, int>(session => queryover.GetExecutableQueryOver(session).RowCount());
        }

        /// <summary>
        /// Returns the number of records of the specified 
        /// type in the database
        /// </summary>
        /// <param name="detachedCriteria">The criteria expression</param>
        /// <returns>The count result</returns>
        public int Count<T>(DetachedCriteria detachedCriteria) where T : class {
            return Execute<T, int>(session => detachedCriteria.GetExecutableCriteria(session)
                                                  .SetProjection(Projections.RowCount()).UniqueResult<int>()
            );
        }

        #endregion

        #region FindFirst

        /// <summary>
        /// Searches and returns the first row for <typeparamref name="T"/>
        /// </summary>
        /// <param name="order">The sort order - used to determine which record is the first one</param>
        /// <param name="criteria">The criteria expression</param>
        /// <returns>A <c>targetType</c> instance or <c>null</c></returns>
        public T FindFirst<T>(Order order, params ICriterion[] criteria) where T : class
        {
            return FindFirst<T>(new[] {order}, criteria);
        }

        /// <summary>
        /// Searches and returns the first row.
        /// </summary>
        /// <param name="orders">The sort order - used to determine which record is the first one</param>
        /// <param name="criterias">The criteria expression</param>
        /// <returns>A <c>targetType</c> instance or <c>null</c></returns>
        public T FindFirst<T>(Order[] orders, params ICriterion[] criterias) where T : class
        {
            return SlicedFindAll<T>(0, 1, orders, criterias).FirstOrDefault();
        }

        /// <summary>
        /// Searches and returns the first row.
        /// </summary>
        /// <param name="criterias">The criteria expression</param>
        /// <returns>A <c>targetType</c> instance or <c>null</c></returns>
        public T FindFirst<T>(params ICriterion[] criterias) where T : class
        {
            return SlicedFindAll<T>(0, 1, criterias).FirstOrDefault();
        }

        /// <summary>
        /// Searches and returns the first row.
        /// </summary>
        /// <param name="detachedCriteria">The criteria.</param>
        /// <param name="orders">The sort order - used to determine which record is the first one.</param>
        /// <returns>A <c>targetType</c> instance or <c>null.</c></returns>
        public T FindFirst<T>(DetachedCriteria detachedCriteria, params Order[] orders) where T : class
        {
            return SlicedFindAll<T>(0, 1, detachedCriteria, orders).FirstOrDefault();
        }

        /// <summary>
        /// Searches and returns the first row.
        /// </summary>
        /// <param name="detachedQuery">The expression query.</param>
        /// <returns>A <c>targetType</c> instance or <c>null.</c></returns>
        public T FindFirst<T>(IDetachedQuery detachedQuery) where T : class
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
        public T FindOne<T>(params ICriterion[] criterias) where T : class
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
        public T FindOne<T>(QueryOver<T,T> queryover) where T : class
        {
            return Execute<T,T>(session => queryover.GetExecutableQueryOver(session).SingleOrDefault());
        }

        /// <summary>
        /// Searches and returns a row. If more than one is found, 
        /// throws <see cref="ActiveRecordException"/>
        /// </summary>
        /// <param name="criteria">The criteria</param>
        /// <returns>A <c>targetType</c> instance or <c>null</c></returns>
        public T FindOne<T>(DetachedCriteria criteria) where T : class
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
        public T FindOne<T>(IDetachedQuery detachedQuery) where T : class
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
        public IEnumerable<T> FindAllByProperty<T>(string property, object value) where T : class
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
        public IEnumerable<T> FindAllByProperty<T>(string orderByColumn, string property, object value) where T : class {
            return Execute<T, IEnumerable<T>>(session =>
                session.CreateCriteria<T>()
                   .Add((value == null) ? Restrictions.IsNull(property) : Restrictions.Eq(property, value))
                   .AddOrder(Order.Asc(orderByColumn))
                .List<T>()
            );
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
        public IEnumerable<T> FindAll<T>(Order order, params ICriterion[] criteria) where T : class
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
        public IEnumerable<T> FindAll<T>(Order[] orders, params ICriterion[] criterias) where T : class {
            return FindAll<T>(DetachedCriteria.For<T>()
                                    .SetResultTransformer(Transformers.DistinctRootEntity)
                                    .AddCriterias(criterias)
                                    .AddOrders(orders)
            );
        }

        /// <summary>
        /// Returns all instances found for the specified type 
        /// using criterias.
        /// </summary>
        /// <param name="criterias"></param>
        /// <returns></returns>
        public IEnumerable<T> FindAll<T>(params ICriterion[] criterias) where T : class
        {
            var query = DetachedCriteria.For<T>()
                .SetResultTransformer(Transformers.DistinctRootEntity)
                .AddCriterias(criterias);
            return Execute<T, IEnumerable<T>>(session => query.GetExecutableCriteria(session).List<T>());
        }

        /// <summary>
        /// Returns all instances found for the specified type according to the criteria
        /// </summary>
        public IEnumerable<T> FindAll<T>(QueryOver<T, T> queryover) where T : class
        {
            return Execute<T, IEnumerable<T>>(session => queryover.GetExecutableQueryOver(session).List<T>());
        }

        /// <summary>
        /// Returns all instances found for the specified type according to the criteria
        /// </summary>
        public IEnumerable<T> FindAll<T>(DetachedCriteria detachedCriteria, params Order[] orders) where T : class {
            detachedCriteria.AddOrders(orders);
            return AR.Execute<T, IEnumerable<T>>(session => detachedCriteria.GetExecutableCriteria(session).List<T>());
        }

        /// <summary>
        /// Returns all instances found for the specified type according to the criteria
        /// </summary>
        /// <param name="detachedQuery">The query expression</param>
        /// <returns>The <see cref="Array"/> of results.</returns>
        public IEnumerable<T> FindAll<T>(IDetachedQuery detachedQuery) where T : class
        {
            return Execute<T, IEnumerable<T>>(session => detachedQuery.GetExecutableQuery(session).List<T>());
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
        public IEnumerable<T> SlicedFindAll<T>(int firstResult, int maxResults, Order order, params ICriterion[] criteria) where T : class
        {
            return SlicedFindAll<T>(firstResult, maxResults, DetachedCriteria.For<T>().AddCriterias(criteria), order);
        }

        /// <summary>
        /// Returns a portion of the query results (sliced)
        /// </summary>
        /// <param name="firstResult">The number of the first row to retrieve.</param>
        /// <param name="maxResults">The maximum number of results retrieved.</param>
        /// <param name="orders">An <see cref="Array"/> of <see cref="NHibernate.Criterion.Order"/> objects.</param>
        /// <param name="criteria">The criteria expression</param>
        /// <returns>The sliced query results.</returns>
        public IEnumerable<T> SlicedFindAll<T>(int firstResult, int maxResults, Order[] orders, params ICriterion[] criteria) where T : class
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
        public IEnumerable<T> SlicedFindAll<T>(int firstResult, int maxResults, params ICriterion[] criteria) where T : class
        {
            return SlicedFindAll<T>(firstResult, maxResults, DetachedCriteria.For<T>().AddCriterias(criteria));
        }

        /// <summary>
        /// Returns a portion of the query results (sliced)
        /// </summary>
        /// <param name="firstResult">The number of the first row to retrieve.</param>
        /// <param name="maxResults">The maximum number of results retrieved.</param>
        /// <param name="orders">An <see cref="Array"/> of <see cref="NHibernate.Criterion.Order"/> objects.</param>
        /// <param name="criteria">The criteria expression</param>
        /// <returns>The sliced query results.</returns>
        public IEnumerable<T> SlicedFindAll<T>(int firstResult, int maxResults, DetachedCriteria criteria, params Order[] orders) where T : class {
            criteria.AddOrders(orders); 
            return AR.Execute<T, IEnumerable<T>>(session => criteria.GetExecutableCriteria(session)
                                                                 .SetFirstResult(firstResult)
                                                                 .SetMaxResults(maxResults)
                                                                 .List<T>());
        }

        /// <summary>
        /// Returns a portion of the query results (sliced)
        /// </summary>
        /// <param name="firstResult">The number of the first row to retrieve.</param>
        /// <param name="maxResults">The maximum number of results retrieved.</param>
        /// <param name="detachedQuery">The query expression</param>
        /// <returns>The sliced query results.</returns>
        public IEnumerable<T> SlicedFindAll<T>(int firstResult, int maxResults, IDetachedQuery detachedQuery) where T : class
        {
            return Execute<T, IEnumerable<T>>(session => detachedQuery.GetExecutableQuery(session)
                .SetFirstResult(firstResult)
                .SetMaxResults(maxResults)
                .List<T>());
        }

        /// <summary>
        /// Returns a portion of the query results (sliced)
        /// </summary>
        /// <param name="firstResult">The number of the first row to retrieve.</param>
        /// <param name="maxResults">The maximum number of results retrieved.</param>
        /// <param name="queryover">Queryover</param>
        /// <returns>The sliced query results.</returns>
        public IEnumerable<T> SlicedFindAll<T>(int firstResult, int maxResults, QueryOver<T, T> queryover) where T : class
        {
            return Execute<T, IEnumerable<T>>(session => queryover.GetExecutableQueryOver(session)
                .Skip(firstResult)
                .Take(maxResults)
                .List<T>());
        }

        #endregion

        #region DeleteAll

        /// <summary>
        /// Deletes all rows for the specified ActiveRecord type that matches
        /// the supplied criteria
        /// </summary>
        public void DeleteAll<T>(DetachedCriteria criteria) where T : class {
            var pks = Execute<T, IEnumerable<object>>(session => criteria.GetExecutableCriteria(session).SetProjection(Projections.Id()).List<object>());
            DeleteAll<T>(pks);
        }

        /// <summary>
        /// Deletes all rows for the specified ActiveRecord type that matches
        /// the supplied criteria
        /// </summary>
        public void DeleteAll<T>(params ICriterion[] criteria) where T : class
        {
            if (criteria != null && criteria.Length > 0)
                DeleteAll<T>(DetachedCriteria.For<T>().AddCriterias(criteria));
            else
                Execute<T>(session => {
                    session.CreateQuery("delete from " + typeof(T).FullName).ExecuteUpdate();
                    session.Flush();
                });
        }

        /// <summary>
        /// Deletes all rows for the specified ActiveRecord type that matches
        /// the supplied expression criteria
        /// </summary>
        public void DeleteAll<T>(Expression<Func<T, bool>> expression) where T : class {
            var pks = Execute<T, IEnumerable<Object>>(session => 
                session.QueryOver<T>().Where(expression).Select(Projections.Id()).List<object>()
            );
            DeleteAll<T>(pks);
        }

        /// <summary>
        /// Deletes all rows for the specified ActiveRecord type that matches
        /// the supplied queryover
        /// </summary>
        public void DeleteAll<T>(QueryOver<T, T> queryover) where T : class {
            var pks = Execute<T, IEnumerable<Object>>(session =>
                queryover.GetExecutableQueryOver(session).Select(Projections.Id()).List<object>()
            );
            DeleteAll<T>(pks);
        }

        /// <summary>
        /// Deletes all rows for the specified ActiveRecord type that matches
        /// the supplied HQL condition
        /// </summary>
        /// <param name="where">HQL condition to select the rows to be deleted</param>
        public void DeleteAll<T>(string where) where T : class
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
        public void DeleteAll<T>(IEnumerable<object> pkvalues) where T : class
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

        #region Save/SaveCopy/Create/Update/Delete

        /// <summary>
        /// Saves the instance to the database. If the primary key is unitialized
        /// it creates the instance on the database. Otherwise it updates it.
        /// <para>
        /// If the primary key is assigned, then you must invoke Create
        /// or Update instead.
        /// </para>
        /// </summary>
        /// <param name="instance">The ActiveRecord instance to be saved</param>
        /// <param name="flush">if set to <c>true</c>, the operation will be followed by a session flush.</param>
        public void Save<T>(T instance, bool flush = true) where T : class
        {
            if (instance == null) throw new ArgumentNullException("instance");
            Execute<T>(session => {
                session.SaveOrUpdate(instance);
                if (flush) session.Flush();
            });
        }

        /// <summary>
        /// Saves a copy of the instance to the database. If the primary key is unitialized
        /// it creates the instance on the database. Otherwise it updates it.
        /// </summary>
        /// <param name="instance">The transient instance to be saved</param>
        /// <param name="flush">if set to <c>true</c>, the operation will be followed by a session flush.</param>
        /// <returns>The saved ActiveRecord instance.</returns>
        public T SaveCopy<T>(T instance, bool flush = true) where T : class
        {
            if (instance == null) throw new ArgumentNullException("instance");
            return Execute<T, T>(session => {
                var persistent = session.Merge(instance);
                if (flush) session.Flush();
                return persistent;
            });
        }

        /// <summary>
        /// Creates (Saves) a new instance to the database.
        /// </summary>
        /// <param name="instance">The ActiveRecord instance to be updated on the database</param>
        /// <param name="flush">if set to <c>true</c>, the operation will be followed by a session flush.</param>
        public object Create<T>(T instance, bool flush = true) where T : class
        {
            if (instance == null) throw new ArgumentNullException("instance");
            return Execute<T, object>(session => {
                var pk = session.Save(instance);
                if (flush) session.Flush();
                return pk;
            });
        }

        /// <summary>
        /// Persists the modification on the instance
        /// state to the database.
        /// </summary>
        /// <param name="instance">The ActiveRecord instance to be updated on the database</param>
        /// <param name="flush">if set to <c>true</c>, the operation will be followed by a session flush.</param>
        public void Update<T>(T instance, bool flush = true) where T : class
        {
            if (instance == null) throw new ArgumentNullException("instance");
            Execute<T>(session => {
                session.Update(instance);

                if (flush) session.Flush();
            });
        }

        /// <summary>
        /// Deletes the instance from the database.
        /// </summary>
        /// <param name="instance">The ActiveRecord instance to be deleted</param>
        /// <param name="flush">if set to <c>true</c>, the operation will be followed by a session flush.</param>
        public void Delete<T>(T instance, bool flush = true) where T : class
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
        public void Refresh<T>(T instance) where T : class
        {
            if (instance == null) throw new ArgumentNullException("instance");
            Execute<T>(session => session.Refresh(instance));
        }

        /// <summary>
        /// Merge the instance to scope session
        /// </summary>
        /// <param name="instance"></param>
        public void Merge<T>(T instance) where T : class
        {
            if (instance == null) throw new ArgumentNullException("instance");
            Execute<T>(session => session.Merge(instance));
        }

        /// <summary>
        /// Evict the instance from scope session
        /// </summary>
        /// <param name="instance"></param>
        public void Evict<T>(object instance) where T : class
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
        public void Replicate<T>(object instance, ReplicationMode replicationMode) where T : class
        {
            if (instance == null) { throw new ArgumentNullException("instance"); }
            Execute<T>(session => session.Replicate(instance, replicationMode));
        }

        #endregion

        #region Linq/QueryOver/Criteria/Query

        /// <summary>
        /// Provide an IQueryable.
        /// Make sure we are in a scope
        /// </summary>
        public IQueryable<T> All<T>() where T : class
        {
            return Execute<T, IQueryable<T>>(s => s.Query<T>());
        }

        /// <summary>
        /// The QueryOver method is used as a Linq collection
        /// or as the in argument in a Linq expression. 
        /// </summary>
        /// <remarks>You must have an open Session Scope.</remarks>
        public IQueryOver<T,T> QueryOver<T>() where T : class
        {
            return Execute<T, IQueryOver<T,T>>(s => s.QueryOver<T>());
        }

        /// <summary>
        /// The QueryOver method is used as a Linq collection
        /// or as the in argument in a Linq expression. 
        /// </summary>
        /// <remarks>You must have an open Session Scope.</remarks>
        public IQueryOver<T,T> QueryOver<T>(Expression<Func<T>> alias) where T : class
        {
            return Execute<T, IQueryOver<T,T>>(s => s.QueryOver<T>(alias));
        }

        /// <summary>
        /// The QueryOver method is used as a Linq collection
        /// or as the in argument in a Linq expression. 
        /// </summary>
        /// <remarks>You must have an open Session Scope.</remarks>
        public IQueryOver<T,T> QueryOver<T>(string entityname) where T : class
        {
            return Execute<T, IQueryOver<T,T>>(s => s.QueryOver<T>(entityname));
        }

        /// <summary>
        /// The QueryOver method is used as a Linq collection
        /// or as the in argument in a Linq expression. 
        /// </summary>
        public IQueryOver<T,T> QueryOver<T>(string entityname, Expression<Func<T>> alias) where T : class
        {
            return Execute<T, IQueryOver<T,T>>(s => s.QueryOver<T>(entityname, alias));
        }

        public IQueryOver<T, T> MakeExecutable<T>(QueryOver<T, T> query) where T : class {
            return Execute<T, IQueryOver<T, T>>(query.GetExecutableQueryOver);
        }

        /// <summary>
        /// Create nhibernate criteria
        /// </summary>
        public ICriteria CreateCriteria<T>() where T : class {
            return Execute<T, ICriteria>(s => s.CreateCriteria<T>());
        }

        public ICriteria MakeExecutable<T>(DetachedCriteria criteria) where T : class {
            return Execute<T, ICriteria>(criteria.GetExecutableCriteria);
        }

        /// <summary>
        /// Create an hql query
        /// </summary>
        public IQuery CreateQuery<T>(string hql) where T : class {
            return Execute<T, IQuery>(s => s.CreateQuery(hql));
        }

        public IQuery MakeExecutable<T>(DetachedQuery query) where T : class {
            return Execute<T, IQuery>(query.GetExecutableQuery);
        }

        /// <summary>
        /// Create a sql query
        /// </summary>
        public ISQLQuery CreateSqlQuery<T>(string sql) where T : class {
            return Execute<T, ISQLQuery>(s => s.CreateSQLQuery(sql));
        }

        #endregion

        #region Execute

        /// <summary>
        /// Invokes the specified delegate passing a valid 
        /// NHibernate session. Used for custom NHibernate queries.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="func">The delegate instance</param>
        /// <param name="instance">The ActiveRecord instance</param>
        /// <returns>Whatever is returned by the delegate invocation</returns>
        public TK Execute<T, TK>(Type type, Func<ISession, T, TK> func, T instance) {
            if (func == null) throw new ArgumentNullException("func", "Delegate must be passed");

            EnsureInitialized(type);

            try {
                return func(OpenSession(type), instance);

            } catch (ObjectNotFoundException ex) {
                FailScope();
                var message = string.Format("Could not find {0} with id {1}", ex.EntityName, ex.Identifier);
                throw new NotFoundException(message, ex);

            } catch (Exception ex) {
                FailScope();
                throw new ActiveRecordException("Error performing Execute for " + type.Name, ex);

            }
        }

        /// <summary>
        /// Invokes the specified delegate passing a valid 
        /// NHibernate session. Used for custom NHibernate queries.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="action">The delegate instance</param>
        public void Execute(Type type, Action<ISession> action) {
            Execute(type, session => {
                action(OpenSession(type));
                return string.Empty;
            });
        }

        /// <summary>
        /// Invokes the specified delegate passing a valid 
        /// NHibernate session. Used for custom NHibernate queries.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="func">The delegate instance</param>
        /// <returns>Whatever is returned by the delegate invocation</returns>
        public TK Execute<TK>(Type type, Func<ISession, TK> func) {
            return Execute<object, TK>(type, (session, arg2) => func(OpenSession(type)), null);
        }

        /// <summary>
        /// Invokes the specified delegate passing a valid 
        /// NHibernate session. Used for custom NHibernate queries.
        /// </summary>
        /// <param name="func">The delegate instance</param>
        /// <param name="instance">The ActiveRecord instance</param>
        /// <returns>Whatever is returned by the delegate invocation</returns>
        public TK Execute<T, TK>(Func<ISession, T, TK> func, T instance) where T : class {
            if (func == null) throw new ArgumentNullException("func", "Delegate must be passed");

            EnsureInitialized(typeof(T));

            try {
                return func(OpenSession<T>(), instance);

            } catch (ObjectNotFoundException ex) {
                HasSessionError = true;
                FailScope();
                var message = string.Format("Could not find {0} with id {1}", ex.EntityName, ex.Identifier);
                throw new NotFoundException(message, ex);

            } catch (Exception ex) {
                HasSessionError = true;
                FailScope();
                throw new ActiveRecordException("Error performing Execute for " + typeof (T).Name, ex);
            }
        }

        /// <summary>
        /// Invokes the specified delegate passing a valid 
        /// NHibernate session. Used for custom NHibernate queries.
        /// </summary>
        /// <param name="action">The delegate instance</param>
        public void Execute<T>(Action<ISession> action) where T : class {
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
        public TK Execute<T, TK>(Func<ISession, TK> func) where T : class {
            return Execute<T, TK>((session, arg2) => func(session), null);
        }

        #endregion

        internal object ConvertId<T>(object id) {
            return ConvertId(typeof (T), id);
        }

        internal object ConvertId(Type type, object id) {
            if (type == null) throw new ArgumentEmptyException("type");
            if (id == null) throw new ArgumentEmptyException("id");

            var pktype = Holder.GetModel(type).PrimaryKey.Value;
            if (pktype.ReturnedClass == id.GetType()) {
                return id;
            }

            if (typeof(ValueType).IsAssignableFrom(pktype.ReturnedClass)) {
                return Convert.ChangeType(id, pktype.ReturnedClass);
            }

            return id;
        }

        internal void EnsureInitialized(Type type)
        {
            if (!Holder.IsInitialized(type))
            {
                throw new ActiveRecordException("No configuration for ActiveRecord found in the type hierarchy -> " + type.FullName);
            }
        }
    }
}
