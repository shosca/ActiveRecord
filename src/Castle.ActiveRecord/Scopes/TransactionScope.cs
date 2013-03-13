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
using System.ComponentModel;
using System.Data;
using Castle.ActiveRecord.Config;
using Castle.Core.Internal;
using NHibernate;

namespace Castle.ActiveRecord.Scopes
{
    /// <summary>
    /// Defines the transaction scope behavior
    /// </summary>
    public enum TransactionMode
    {
        /// <summary>
        /// Inherits a transaction previously create on 
        /// the current context.
        /// </summary>
        Inherits,
        /// <summary>
        /// Always create an isolated transaction context.
        /// </summary>
        New
    }

    /// <summary>
    /// Governs the <see cref="TransactionScope"/> behavior 
    /// on dispose if neither <see cref="TransactionScope.VoteCommit"/>
    /// nor <see cref="TransactionScope.VoteRollBack"/> was called
    /// </summary>
    public enum OnDispose
    {
        /// <summary>
        /// Should commit the transaction, unless <see cref="TransactionScope.VoteRollBack"/>
        /// was called before the disposing the scope (this is the default behavior)
        /// </summary>
        Commit,
        /// <summary>
        /// Should rollback the transaction, unless <see cref="TransactionScope.VoteCommit"/>
        /// was called before the disposing the scope
        /// </summary>
        Rollback
    }

    /// <summary>
    /// Implementation of <see cref="ISessionScope"/> to 
    /// provide transaction semantics
    /// </summary>
    public class TransactionScope : SessionScope
    {
        private static readonly object CompletedEvent = new object();

        private readonly TransactionMode mode;
        private readonly TransactionScope parentTransactionScope;
        private readonly EventHandlerList events = new EventHandlerList();

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionScope"/> class.
        /// </summary>
        public TransactionScope(
            TransactionMode mode = TransactionMode.New,
            IsolationLevel isolation = IsolationLevel.Unspecified,
            OnDispose ondispose = OnDispose.Commit,
            ISessionFactoryHolder holder = null
            ) : base(FlushAction.Config, SessionScopeType.Transactional, isolation, ondispose, holder)
        {
            this.mode = mode;

            parentTransactionScope = ParentScope as TransactionScope;

            if (mode == TransactionMode.New) {
                if (parentTransactionScope != null) {
                    parentTransactionScope = null;
                    ParentScope = null;
                } else {
                    parentTransactionScope = null;
                }
            }
        }

        #region OnTransactionCompleted event

        /// <summary>
        /// This event is raised when a transaction is completed
        /// </summary>
        public event EventHandler OnTransactionCompleted {
            add {
                if (parentTransactionScope != null) {
                    parentTransactionScope.OnTransactionCompleted += value;
                } else {
                    events.AddHandler(CompletedEvent, value);
                }
            }
            remove {
                if (parentTransactionScope != null) {
                    parentTransactionScope.OnTransactionCompleted -= value;
                } else {
                    events.RemoveHandler(CompletedEvent, value);
                }
            }
        }

        #endregion

        /// <summary>
        /// Votes to roll back the transaction
        /// </summary>
        public override void VoteRollBack() {
            if (mode == TransactionMode.Inherits && parentTransactionScope != null) {
                parentTransactionScope.VoteRollBack();
            }
            Rollback = true;
        }

        /// <summary>
        /// Votes to commit the transaction
        /// </summary>
        public override void VoteCommit()
        {
            if (Rollback) {
                throw new TransactionException("The transaction was marked as rollback only" +
                                               " - by itself or one of the nested transactions");
            }
            SetForCommit = true;
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
        public override bool IsKeyKnown(object key)
        {
            if (parentTransactionScope != null) {
                return parentTransactionScope.IsKeyKnown(key);
            }
            if (ParentScope != null) {
                return ParentScope.IsKeyKnown(key);
            }
            return Key2Session.ContainsKey(key);
        }

        public override void RegisterSession(object key, ISession session) {
            if (parentTransactionScope != null) {
                parentTransactionScope.RegisterSession(key, session);
                return;
            }
            if (ParentScope != null) {
                ParentScope.RegisterSession(key, session);
            }
            if (!Key2Session.ContainsKey(key))
                Key2Session.Add(key, session);
        }

        public override ISession GetSession(object key) {
            if (parentTransactionScope != null) {
                return parentTransactionScope.GetSession(key);
            }

            var session = ParentScope == null
                       ? Key2Session[key]
                       : ParentScope.GetSession(key);

            if (!Key2Session.ContainsKey(key))
                Key2Session.Add(key, session);

            Initialize(session);
            return session;
        }


        public override void Dispose() {
            Holder.ThreadScopeInfo.UnRegisterScope(this);

            PerformDisposal(ParentScope == null, parentTransactionScope == null);

            RaiseOnCompleted();
#if DEBUG
            System.Diagnostics.Debug.Assert(Key2Session.Count == 0);
#endif
        }

        public override void Flush() {
            if (ParentScope != null) {
                ParentScope.Flush();
            }

            base.Flush();
        }

        /// <summary>
        /// This is called when a session has a failure
        /// </summary>
        public override void FailScope() {
            if (ParentScope != null) {
                ParentScope.FailScope();
            }
            base.FailScope();
        }

        /// <summary>
        /// Raises the on completed event
        /// </summary>
        private void RaiseOnCompleted() {
            var handler = events[CompletedEvent] as EventHandler;

            if (handler != null) {
                handler(this, EventArgs.Empty);
            }
        }
    }
}
