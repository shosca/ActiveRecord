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
using System.Data;
using NHibernate;

namespace Castle.ActiveRecord.Scopes {
    /// <summary>
    /// Still very experimental and it's not bullet proof
    /// for all situations
    /// </summary>
    public class DifferentDatabaseScope : SessionScope {
        private readonly IDbConnection _connection;

        /// <summary>
        /// Initializes a new instance of the <see cref="DifferentDatabaseScope"/> class.
        /// </summary>
        public DifferentDatabaseScope(
            IDbConnection connection,
            FlushAction flushAction = FlushAction.Auto,
            ISessionFactoryHolder holder = null
            )
            : base(flushAction, holder: holder) {
            if (connection == null) throw new ArgumentNullException("connection");

            _connection = connection;

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
        public override void RegisterSession(object key, ISession session) {
            if (ParentScope != null) {
                ParentScope.RegisterSession(new KeyHolder(key, _connection.ConnectionString, _connection.GetHashCode()), session);
                return;
            }

            base.RegisterSession(key, session);
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
        public override bool IsKeyKnown(object key) {
            if (ParentScope != null) {
                return ParentScope.IsKeyKnown(new KeyHolder(key, _connection.ConnectionString, _connection.GetHashCode()));
            }

            return base.IsKeyKnown(key);
        }

        /// <summary>
        /// This method should return the session instance associated with the key.
        /// </summary>
        /// <param name="key">an object instance</param>
        /// <returns>
        /// the session instance or null if none was found
        /// </returns>
        public override ISession GetSession(object key) {
            if (ParentScope != null) {
                return ParentScope.GetSession(new KeyHolder(key, _connection.ConnectionString, _connection.GetHashCode()));
            }

            return base.GetSession(key);
        }

        public override void Dispose() {
            bool flush = !HasSessionError && this.FlushAction != FlushAction.Never;

            if (ParentScope == null) {
                PerformDisposal(flush);
            }

            base.Dispose();
#if DEBUG
            System.Diagnostics.Debug.Assert(Key2Session.Count == 0);
#endif
        }

        /// <summary>
        /// This method is invoked to allow
        /// the scope to create a properly configured session
        /// </summary>
        /// <param name="sessionFactory">From where to open the session</param>
        /// <param name="interceptor">the NHibernate interceptor</param>
        /// <returns>the newly created session</returns>
        protected override ISession CreateSession(ISessionFactory sessionFactory, IInterceptor interceptor) {
            return sessionFactory.OpenSession(_connection, interceptor);
        }

        /// <summary>
        /// This is called when a scope has a failure
        /// </summary>
        public override void FailScope() {
            if (ParentScope != null) {
                ParentScope.FailScope();
            }
            base.FailScope();
        }
    }

    internal class KeyHolder {
        private readonly object _key;
        private readonly String _connectionString;
        private readonly int _connectionHashCode;

        public KeyHolder(object key, String connectionString, int connectionHashCode) {
            this._key = key;
            this._connectionHashCode = connectionHashCode;
            this._connectionString = connectionString;
        }

        public override bool Equals(object obj) {
            var other = obj as KeyHolder;

            if (other != null) {
                return ReferenceEquals(_key, other._key) &&
                       _connectionString == other._connectionString &&
                       _connectionHashCode == other._connectionHashCode;
            }

            return false;
        }

        public override int GetHashCode() {
            return _key.GetHashCode() ^ _connectionString.GetHashCode() ^ _connectionHashCode;
        }
    }
}
