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

using System.Linq;
using Castle.ActiveRecord.Scopes;
using Castle.ActiveRecord.Tests.Models;
using NHibernate.Linq;

namespace Castle.ActiveRecord.Tests
{
	using System.Collections;
	using System.Collections.Generic;
	using NUnit.Framework;
	using Castle.ActiveRecord;
	using NHibernate;

	[TestFixture]
	public class SessionScopeAutoflushTestCase : AbstractActiveRecordTest
	{
		[Test]
		public void ActiveRecordUsingSessionScope()
		{
			using (new SessionScope())
			{
				new SSAFEntity("example").Save();
				Assert.AreEqual(1, SSAFEntity.FindAll().Count());
			}
			Assert.AreEqual(1, SSAFEntity.FindAll().Count());
		}

		public void ActiveRecordUsingNoFlushSessionScope()
		{
			using (new SessionScope(FlushAction.Never))
			{
				new SSAFEntity("example").Save();
				Assert.AreEqual(0, SSAFEntity.FindAll().Count());
			}
			Assert.AreEqual(0, SSAFEntity.FindAll().Count());
		}

		[Test]
		public void ActiveRecordUsingSessionScopeUsingExplicitFlush()
		{
			using (new SessionScope())
			{
				new SSAFEntity("example").Save();
				SessionScope.Current.Flush();
				Assert.AreEqual(1, SSAFEntity.FindAll().Count());
			}
			Assert.AreEqual(1, SSAFEntity.FindAll().Count());
		}

		[Test]
		public void ActiveRecordUsingTransactionScope()
		{
			using (new TransactionScope())
			{
				new SSAFEntity("example").Save();
				//Assert.AreEqual(1, SSAFEntity.FindAll().Length);
			}
			Assert.AreEqual(1, SSAFEntity.FindAll().Count());
		}

		[Test]
		public void ActiveRecordUsingTransactionScopeWithRollback()
		{
			using (TransactionScope scope = new TransactionScope())
			{
				new SSAFEntity("example").Save();
				//Assert.AreEqual(1, SSAFEntity.FindAll().Length);
				scope.VoteRollBack();
			}
			Assert.AreEqual(0, SSAFEntity.FindAll().Count());
		}

		[Test][Ignore("Need to clean up scopes.")]
		public void ActiveRecordUsingTransactionScopeWithRollbackAndInnerSessionScope()
		{
			using (TransactionScope scope = new TransactionScope())
			{
				using (new SessionScope())
				{
					new SSAFEntity("example").SaveAndFlush();
					Assert.AreEqual(1, SSAFEntity.FindAll().Count());
				}
				Assert.AreEqual(1, SSAFEntity.FindAll().Count());
				scope.VoteRollBack();
			}
			Assert.AreEqual(0, SSAFEntity.FindAll().Count());
		}

		[Test]
		public void ActiveRecordUsingNestedTransactionScopesWithRollback()
		{
			using (TransactionScope scope = new TransactionScope())
			{
				using (new TransactionScope(TransactionMode.Inherits))
				{
					new SSAFEntity("example").SaveAndFlush();
					Assert.AreEqual(1, SSAFEntity.FindAll().Count());
				}
				Assert.AreEqual(1, SSAFEntity.FindAll().Count());
				scope.VoteRollBack();
			}
			Assert.AreEqual(0, SSAFEntity.FindAll().Count());
		}

		[Test]
		public void ActiveRecordUsingTransactionScopeWithCommitAndInnerSessionScope()
		{
			using (TransactionScope scope = new TransactionScope())
			{
				using (new SessionScope())
				{
					new SSAFEntity("example").SaveAndFlush();
					Assert.AreEqual(1, SSAFEntity.FindAll().Count());
				}
				//Assert.AreEqual(1, SSAFEntity.FindAll().Length);
				scope.VoteCommit();
			}
			Assert.AreEqual(1, SSAFEntity.FindAll().Count());
		}


		[Test]
		public void NHibernateVerification()
		{
			ActiveRecord<SSAFEntity>.Execute(session => {
				using (session.BeginTransaction())
				{
					session.Save(new SSAFEntity("example"));
					Assert.AreEqual(1, session.CreateQuery("from " + typeof(SSAFEntity).FullName).List<SSAFEntity>().Count);
				}
			});
		}

		[Test]
		public void SessionTxVerification()
		{
			ActiveRecord<SSAFEntity>.Execute(session => {
				using (ITransaction tx = session.BeginTransaction())
				{
					Assert.AreSame(tx, session.BeginTransaction());
					Assert.AreSame(tx, session.Transaction);
				}
			});
		}


		[Test]
		public void NHibernateNoTxVerification()
		{
			using (ISession session = ActiveRecord.Holder.GetSessionFactory(typeof(SSAFEntity)).OpenSession())
			{
				session.Save(new SSAFEntity("example"));
				session.Flush();
				Assert.AreEqual(1, session.Query<SSAFEntity>().ToList<SSAFEntity>().Count);
			}
		}
	}
}
