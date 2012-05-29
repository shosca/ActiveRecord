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
			InitModel();
			using (new SessionScope())
			{
				new SSAFEntity("example").Save();
				Assert.AreEqual(1, SSAFEntity.FindAll().Count());
			}
			Assert.AreEqual(1, SSAFEntity.FindAll().Count());
		}

		public void ActiveRecordUsingNoFlushSessionScope()
		{
			InitModel();
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
			InitModel();
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
			InitModel();
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
			InitModel();
			using (TransactionScope scope = new TransactionScope())
			{
				new SSAFEntity("example").Save();
				//Assert.AreEqual(1, SSAFEntity.FindAll().Length);
				scope.VoteRollBack();
			}
			Assert.AreEqual(0, SSAFEntity.FindAll().Count());
		}

		[Test][Ignore("This is worth debate")]
		public void ActiveRecordUsingTransactionScopeWithRollbackAndInnerSessionScope()
		{
			InitModel();
			using (TransactionScope scope = new TransactionScope())
			{
				using (new SessionScope())
				{
					new SSAFEntity("example").Save();
					Assert.AreEqual(1, SSAFEntity.FindAll().Count());
				}
				Assert.AreEqual(1, SSAFEntity.FindAll().Count());
				scope.VoteRollBack();
			}
			Assert.AreEqual(0, SSAFEntity.FindAll().Count());
		}

		public void ActiveRecordUsingNestedTransactionScopesWithRollback()
		{
			InitModel();
			using (TransactionScope scope = new TransactionScope())
			{
				using (new TransactionScope(TransactionMode.Inherits))
				{
					new SSAFEntity("example").Save();
					Assert.AreEqual(1, SSAFEntity.FindAll().Count());
				}
				Assert.AreEqual(1, SSAFEntity.FindAll().Count());
				scope.VoteRollBack();
			}
			Assert.AreEqual(0, SSAFEntity.FindAll().Count());
		}

		public void ActiveRecordUsingTransactionScopeWithCommitAndInnerSessionScope()
		{
			InitModel();
			using (TransactionScope scope = new TransactionScope())
			{
				using (new SessionScope())
				{
					new SSAFEntity("example").Save();
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
			InitModel();
			using (ISession session = ActiveRecord.Holder.GetSessionFactory(typeof(ActiveRecord)).OpenSession())
			using (ITransaction tx = session.BeginTransaction())
			{
				session.Save(new SSAFEntity("example"));
				Assert.AreEqual(1, session.CreateQuery("from SSAFEntity").List<SSAFEntity>().Count);
			}
		}

		[Test]
		public void SessionTxVerification()
		{
			InitModel();
			using (ISession session = ActiveRecord.Holder.GetSessionFactory(typeof(ActiveRecord)).OpenSession())
			{
				Assert.IsFalse(session.Transaction.IsActive);
				using (ITransaction tx = session.BeginTransaction())
				{
					Assert.AreSame(tx, session.BeginTransaction());
					Assert.AreSame(tx, session.Transaction);
				}
			}
		}


		[Test]
		[Ignore("Expected to fail")]
		public void NHibernateNoTxVerification()
		{
			InitModel();
			using (ISession session = ActiveRecord.Holder.GetSessionFactory(typeof(ActiveRecord)).OpenSession())
			{
				session.Save(new SSAFEntity("example"));
				Assert.AreEqual(1, session.CreateQuery("from SSAFEntity").List<SSAFEntity>().Count);
			}
		}

		private void InitModel()
		{
			ActiveRecord.Initialize(GetConfigSource());
			Recreate();
		}

	}

	#region Model

	#endregion
}