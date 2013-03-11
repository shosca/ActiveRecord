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
using NHibernate.Linq;
using NHibernate.Transform;

namespace Castle.ActiveRecord.Scopes
{
    /// <summary>
    /// Implementation of <see cref="ISessionScope"/> to 
    /// augment performance by caching the session, thus
    /// avoiding too much opens/flushes/closes.
    /// </summary>
    public class SessionScope : MarshalByRefObject, ISessionScope
    {
        private readonly SessionScopeType type;

        private readonly FlushAction flushAction;

        protected ISessionFactoryHolder holder;

        /// <summary>
        /// Map between a key to its session
        /// </summary>
        protected IDictionary<object, ISession> key2Session = new Dictionary<object, ISession>();

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionScope"/> class.
        /// </summary>
        /// <param name="flushAction">The flush action.</param>
        /// <param name="type">The type.</param>
        public SessionScope(FlushAction flushAction = FlushAction.Config, SessionScopeType type = SessionScopeType.Simple, ISessionFactoryHolder holder = null)
        {
            this.flushAction = flushAction;
            this.type = type;
            HasSessionError = false;

            this.holder = holder ?? AR.Holder;

            AR.Holder.ThreadScopeInfo.RegisterScope(this);
        }

        /// <summary>
        /// Returns the <see cref="SessionScopeType"/> defined 
        /// for this scope
        /// </summary>
        public SessionScopeType ScopeType { get { return type; } }

        /// <summary>
        /// Returns the <see cref="ISessionScope.FlushAction"/> defined 
        /// for this scope
        /// </summary>
        public FlushAction FlushAction { get { return flushAction; } }

        /// <summary>
        /// Flushes the sessions that this scope 
        /// is maintaining
        /// </summary>
        public virtual void Flush()
        {
            foreach (ISession session in GetSessions())
            {
                session.Flush();
            }
        }

        /// <summary>
        /// This method is invoked when the
        /// <see cref="ISessionFactoryHolder"/>
        /// instance needs a session instance. Instead of creating one it interrogates
        /// the active scope for one. The scope implementation must check if it
        /// has a session registered for the given key.
        /// <seealso cref="RegisterSession"/>
        /// </summary>
        /// <param name="key">an object instance</param>
        /// <returns>
        ///     <c>true</c> if the key exists within this scope instance
        /// </returns>
        public virtual bool IsKeyKnown(object key) { return key2Session.ContainsKey(key); }

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
        public virtual void RegisterSession(object key, ISession session)
        {
            key2Session.Add(key, session);
            Initialize(session);
        }

        /// <summary>
        /// This method should return the session instance associated with the key.
        /// </summary>
        /// <param name="key">an object instance</param>
        /// <returns>
        /// the session instance or null if none was found
        /// </returns>
        public virtual ISession GetSession(object key)
        {
            var session = key2Session[key];
            if (session != null && FlushAction != FlushAction.Never)
                if (session.Transaction == null || !session.Transaction.IsActive)
                    session.BeginTransaction();
            return key2Session[key];
        }

        /// <summary>
        /// This method is invoked to allow the scope to create a properly configured session
        /// </summary>
        /// <param name="sessionFactory">From where to open the session</param>
        /// <param name="interceptor">the NHibernate interceptor</param>
        /// <returns>the newly created session</returns>
        protected virtual ISession OpenSession(ISessionFactory sessionFactory, IInterceptor interceptor)
        {
            ISession session = sessionFactory.OpenSession(interceptor);
            SetFlushMode(session);

            if (FlushAction != FlushAction.Never)
                session.BeginTransaction();

            return session;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            holder.ThreadScopeInfo.UnRegisterScope(this);

            PerformDisposal(key2Session.Values);
        }

        /// <summary>
        /// Initializes the specified session.
        /// </summary>
        /// <param name="session">The session.</param>
        protected virtual void Initialize(ISession session) { }

        /// <summary>
        /// Performs the disposal.
        /// </summary>
        /// <param name="sessions">The sessions.</param>
        protected virtual void PerformDisposal(ICollection<ISession> sessions)
        {
            if (HasSessionError || FlushAction == FlushAction.Never)
            {
                PerformDisposal(sessions, false, true);
            }
            else
            {
                PerformDisposal(sessions, true, true);
            }
        }

        /// <summary>
        /// Performs the disposal.
        /// </summary>
        /// <param name="sessions">The sessions.</param>
        /// <param name="flush">if set to <c>true</c> [flush].</param>
        /// <param name="close">if set to <c>true</c> [close].</param>
        protected internal void PerformDisposal(ICollection<ISession> sessions, bool flush, bool close)
        {
            foreach (var session in sessions)
            {
                var commit = true;
                try
                {
                    if (flush)
                    {
                        session.Flush();
                    }
                }
                catch
                {
                    commit = false;
                    throw;
                }
                finally
                {
                    var tx = session.Transaction;
                    if (session.IsConnected &&
                        session.Connection.State == ConnectionState.Open &&
                        tx != null &&
                        tx.IsActive &&
                        !(tx.WasCommitted || tx.WasRolledBack))
                    {
                        if (commit && !HasSessionError)
                            tx.Commit();
                        else
                            tx.Rollback();
                        tx.Dispose();
                    }

                    if (close) session.Dispose();
                }
            }
        }

        /// <summary>
        /// Gets or sets a flag indicating whether this instance has session error.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this instance has session error; otherwise, <c>false</c>.
        /// </value>
        public bool HasSessionError { get; private set; }

        /// <summary>
        /// Discards the sessions.
        /// </summary>
        /// <param name="sessions">The sessions.</param>
        protected internal virtual void DiscardSessions(ICollection<ISession> sessions)
        {
            foreach (ISession session in sessions)
            {
                RemoveSession(session);
            }
        }

        /// <summary>
        /// This method will be called if a scope action fails. 
        /// The scope may then decide to use an different approach to flush/dispose it.
        /// </summary>
        public virtual void FailScope() {
            HasSessionError = true;
        }


        #region Find/Peek

        /// <summary>
        /// Finds an object instance by its primary key
        /// returns null if not found
        /// </summary>
        /// <param name="id">Identifier value</param>
        public T Find<T>(object id) where T : class
        {
            return id == null ? null : Execute<T, T>(session => session.Get<T>(AR.ConvertId<T>(id)));
        }

        /// <summary>
        /// Peeks for an object instance by its primary key,
        /// never returns null
        /// </summary>
        /// <param name="id">Identifier value</param>
        public T Peek<T>(object id) where T : class
        {
            return Execute<T, T>(session => session.Load<T>(AR.ConvertId<T>(id)));
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
        public int Count<T>(Expression<Func<T, bool>> expression) where T : class
        {
            return NHibernate.Criterion.QueryOver.Of<T>().Where(expression).Count();
        }

        /// <summary>
        /// Returns the number of records of the specified 
        /// type in the database
        /// </summary>
        /// <param name="queryover">The criteria expression</param>
        /// <returns>The count result</returns>
        public int Count<T>(QueryOver<T, T> queryover) where T : class
        {
            return queryover.Count();
        }

        /// <summary>
        /// Returns the number of records of the specified 
        /// type in the database
        /// </summary>
        /// <param name="detachedCriteria">The criteria expression</param>
        /// <returns>The count result</returns>
        public int Count<T>(DetachedCriteria detachedCriteria) where T : class
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
            return queryover.FindOne();
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
        public IEnumerable<T> FindAllByProperty<T>(string orderByColumn, string property, object value) where T : class
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
        public IEnumerable<T> FindAll<T>(Order[] orders, params ICriterion[] criterias) where T : class 
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
        public IEnumerable<T> FindAll<T>(params ICriterion[] criterias) where T : class
        {
            return DetachedCriteria.For<T>()
                .SetResultTransformer(Transformers.DistinctRootEntity)
                .AddCriterias(criterias)
                .List<T>();
        }

        /// <summary>
        /// Returns all instances found for the specified type according to the criteria
        /// </summary>
        public IEnumerable<T> FindAll<T>(QueryOver<T, T> queryover) where T : class
        {
            return queryover.List();
        }

        /// <summary>
        /// Returns all instances found for the specified type according to the criteria
        /// </summary>
        public IEnumerable<T> FindAll<T>(DetachedCriteria detachedCriteria, params Order[] orders) where T : class
        {
            return detachedCriteria.AddOrders(orders).List<T>();
        }

        /// <summary>
        /// Returns all instances found for the specified type according to the criteria
        /// </summary>
        /// <param name="detachedQuery">The query expression</param>
        /// <returns>The <see cref="Array"/> of results.</returns>
        public IEnumerable<T> FindAll<T>(IDetachedQuery detachedQuery) where T : class
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
        public IEnumerable<T> SlicedFindAll<T>(int firstResult, int maxResults, DetachedCriteria criteria, params Order[] orders) where T : class
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
        public IEnumerable<T> SlicedFindAll<T>(int firstResult, int maxResults, IDetachedQuery detachedQuery) where T : class
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
        public IEnumerable<T> SlicedFindAll<T>(int firstResult, int maxResults, QueryOver<T, T> queryover) where T : class
        {
            return queryover.SlicedFindAll<T>(firstResult, maxResults);
        }

        #endregion


        #region DeleteAll

        /// <summary>
        /// Deletes all rows for the specified ActiveRecord type that matches
        /// the supplied criteria
        /// </summary>
        public void DeleteAll<T>(DetachedCriteria criteria) where T : class
        {
            var pks = criteria.SetProjection(Projections.Id()).List<T, object>();
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
        public void DeleteAll<T>(Expression<Func<T, bool>> expression) where T : class
        {
            var pks = NHibernate.Criterion.QueryOver.Of<T>().Where(expression).Select(Projections.Id()).List<T, object>();
            DeleteAll<T>(pks);
        }

        /// <summary>
        /// Deletes all rows for the specified ActiveRecord type that matches
        /// the supplied queryover
        /// </summary>
        public void DeleteAll<T>(QueryOver<T, T> queryover) where T : class
        {
            var pks = queryover.Select(Projections.Id()).List<T, object>();
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
            var cm = holder.GetSessionFactory(typeof (T)).GetClassMetadata(typeof (T));
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

        #region Linq/QueryOver

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
        public IQueryOver<T> QueryOver<T>() where T : class
        {
            return Execute<T, IQueryOver<T>>(s => s.QueryOver<T>());
        }

        /// <summary>
        /// The QueryOver method is used as a Linq collection
        /// or as the in argument in a Linq expression. 
        /// </summary>
        /// <remarks>You must have an open Session Scope.</remarks>
        public IQueryOver<T> QueryOver<T>(Expression<Func<T>> alias) where T : class
        {
            return Execute<T, IQueryOver<T>>(s => s.QueryOver<T>(alias));
        }

        /// <summary>
        /// The QueryOver method is used as a Linq collection
        /// or as the in argument in a Linq expression. 
        /// </summary>
        /// <remarks>You must have an open Session Scope.</remarks>
        public IQueryOver<T> QueryOver<T>(string entityname) where T : class
        {
            return Execute<T, IQueryOver<T>>(s => s.QueryOver<T>(entityname));
        }

        /// <summary>
        /// The QueryOver method is used as a Linq collection
        /// or as the in argument in a Linq expression. 
        /// </summary>
        /// <remarks>You must have an open Session Scope.</remarks>
        public IQueryOver<T> QueryOver<T>(string entityname, Expression<Func<T>> alias) where T : class
        {
            return Execute<T, IQueryOver<T>>(s => s.QueryOver<T>(entityname, alias));
        }

        #endregion


        /*
        public IQueryOver<T> GetExecutableQueryOver<T>(QueryOver<T> query) {
            return query.GetExecutableQueryOver(CreateSession<T>());
        }

        public ICriteria GetExecutableCriteria<T>(DetachedCriteria query) {
            return query.GetExecutableCriteria(CreateSession<T>());
        }

        public IQuery GetExecutableQuery<T>(IDetachedQuery query) {
            return query.GetExecutableQuery(CreateSession<T>());
        }

        public T Find<T>(object id) where T : class {
            return id == null ? null : Execute<T, T>(session => session.Get<T>(ConvertId<T>(id)));
        }

        public T Get<T>(object convertId) {
            return CreateSession<T>().Get<T>(convertId);
        }

        public T Load<T>(object convertId) {
            return CreateSession<T>().Load<T>(convertId);
        }

        public object Get(Type type, object convertId) {
            return CreateScopeSession(type).Get(type, convertId);
        }

        public object Load(Type type, object convertId) {
            return CreateScopeSession(type).Load(type, convertId);
        }

        public IQuery CreateQuery<T>(string s) {
            return CreateSession<T>().CreateQuery(s);
        }

        public void Delete<T>(string where) {
            CreateSession<T>().Delete(where);
        }

        public T Merge<T>(T instance) where T : class {
            return CreateSession<T>().Merge(instance);
        }

        public object Save<T>(T instance) {
            return CreateSession<T>().Save(instance);
        }

        public void SaveOrUpdate<T>(T instance) {
            CreateSession<T>().SaveOrUpdate(instance);
        }

        public void Update<T>(T instance) {
            CreateSession<T>().Update(instance);
        }

        public void Delete<T>(T instance) {
            CreateSession<T>().Delete(instance);
        }

        public void Refresh<T>(T instance) {
            CreateSession<T>().Refresh(instance);
        }

        public void Replicate<T>(T instance, ReplicationMode replicationmode) {
            CreateSession<T>().Replicate(instance, replicationmode);
        }

        public IQueryable<T> Query<T>() {
            return CreateSession<T>().Query<T>();
        }
        */

        #region Execute/ExecuteStateless

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

            AR.EnsureInitialized(type);

            try {
                return func(CreateScopeSession(type), instance);

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
                action(CreateScopeSession(type));
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
            return Execute<object, TK>(type, (session, arg2) => func(CreateScopeSession(type)), null);
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

            AR.EnsureInitialized(typeof(T));

            try {
                return func(CreateSession<T>(), instance);

            } catch (ObjectNotFoundException ex) {
                FailScope();
                var message = string.Format("Could not find {0} with id {1}", ex.EntityName, ex.Identifier);
                throw new NotFoundException(message, ex);

            } catch (Exception ex) {
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

        /// <summary>
        /// Sets the flush mode.
        /// </summary>
        /// <param name="session">The session.</param>
        protected void SetFlushMode(ISession session)
        {
            if (FlushAction == FlushAction.Auto)
            {
                session.FlushMode = FlushMode.Auto;
            }
            else if (FlushAction == FlushAction.Never)
            {
                session.FlushMode = FlushMode.Never;
            }
            else if (FlushAction == FlushAction.Config)
            {
                DefaultFlushType behaviour = holder.ConfigurationSource.DefaultFlushType;
                session.FlushMode = (behaviour == DefaultFlushType.Classic || behaviour == DefaultFlushType.Auto) ?
                    FlushMode.Auto :
                    (behaviour == DefaultFlushType.Leave) ?
                    FlushMode.Commit :
                    FlushMode.Never;
            }
        }

        /// <summary>
        /// Notifies the scope that an inner scope that changed the flush mode, was
        /// disposed. The scope should reset the flush mode to its default.
        /// </summary>
        protected internal void ResetFlushMode()
        {
            foreach (ISession session in GetSessions())
            {
                SetFlushMode(session);
            }
        }

        /// <summary>
        /// Gets the sessions.
        /// </summary>
        /// <returns></returns>
        public ICollection<ISession> GetSessions()
        {
            return key2Session.Values;
        }

        /// <summary>
        /// Removes the session.
        /// </summary>
        /// <param name="session">The session.</param>
        protected virtual void RemoveSession(ISession session)
        {
            var exceptions = new List<Exception>();
            foreach (var key in key2Session.Keys.ToArray())
            {
                if (ReferenceEquals(key2Session[key], session))
                {
                    try {
                        session.Close();
                        key2Session.Remove(key);
                    } catch (Exception e) {
                        exceptions.Add(e);
                    }
                }
            }
            if (exceptions.Count > 0) {
                throw new AggregateException(exceptions);
            }
        }

        protected ISessionScope FindPreviousScope(bool preferenceForTransactional)
        {
            return FindPreviousScope(preferenceForTransactional, false);
        }

        protected ISessionScope FindPreviousScope(bool preferenceForTransactional, bool doNotReturnSessionScope)
        {
            object[] items = holder.ThreadScopeInfo.CurrentStack.ToArray();

            ISessionScope first = null;

            for (int i = 0; i < items.Length; i++)
            {
                ISessionScope scope = items[i] as ISessionScope;

                if (scope == this) continue;

                if (first == null) first = scope;

                if (!preferenceForTransactional) break;

                if (scope.ScopeType == SessionScopeType.Transactional)
                {
                    return scope;
                }
            }

            return doNotReturnSessionScope ? null : first;
        }

        /// <summary>
        /// Creates a session for the associated type
        /// </summary>
        public virtual ISession CreateSession<T>()
        {
            return CreateScopeSession(typeof(T));
        }

        protected virtual ISession OpenSessionWithScope(ISessionFactory sessionFactory)
        {
            lock(sessionFactory)
            {
                return OpenSession(sessionFactory, InterceptorFactory.Create());
            }
        }

        protected virtual ISession CreateScopeSession(Type type)
        {
            var sessionFactory = holder.GetSessionFactory(type);
#if DEBUG
            System.Diagnostics.Debug.Assert(sessionFactory != null);
#endif
            if (IsKeyKnown(sessionFactory))
            {
                return GetSession(sessionFactory);
            }

            var session = OpenSessionWithScope(sessionFactory);
#if DEBUG
            System.Diagnostics.Debug.Assert(session != null);
#endif
            RegisterSession(sessionFactory, session);

            return session;
        }
    }
}
