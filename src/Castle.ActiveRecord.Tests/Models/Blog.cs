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
using Castle.ActiveRecord.Tests.Model;
using Iesi.Collections.Generic;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace Castle.ActiveRecord.Tests.Models
{
	public class BlogMapping : ClassMapping<Blog> {
		public BlogMapping() {
			Id(x => x.Id, m => m.Generator(Generators.Native));
			Set(x => x.Posts, m => {
				m.Inverse(true);
				m.Lazy(CollectionLazy.Lazy);
			});
			Set(x => x.PublishedPosts, m => {
				m.Inverse(true);
				m.Lazy(CollectionLazy.Lazy);
				m.Where("published = 1");
			});
			Set(x => x.UnPublishedPosts, m => {
				m.Inverse(true);
				m.Lazy(CollectionLazy.Lazy);
				m.Where("published = 0");
			});
			Set(x => x.RecentPosts, m => {
				m.Inverse(true);
				m.Lazy(CollectionLazy.Lazy);
				m.OrderBy(p => p.Created);
			});
		}	
	}

	public class PostMapping : ClassMapping<Post> {
		public PostMapping() {
			Id(x => x.Id, m => m.Generator(Generators.Native));
		}	
	}

	public class Blog : ActiveRecordBase<Blog>
	{
		private bool onSaveCalled, onUpdateCalled, onDeleteCalled, onLoadCalled;

		public Blog()
		{
			Posts = new HashedSet<Post>();
			PublishedPosts = new HashedSet<Post>();
			UnPublishedPosts = new HashedSet<Post>();
			RecentPosts = new HashedSet<Post>();
		}

		public Blog(int _id)
		{
			this.Id = _id;
		}

		public virtual int Id { get; set; }

		public virtual string Name { get; set; }

		public virtual string Author { get; set; }

		public virtual ISet<Post> Posts { get; set; }
		public virtual ISet<Post> PublishedPosts { get; set; }
		public virtual ISet<Post> UnPublishedPosts { get; set; }
		public virtual ISet<Post> RecentPosts { get; set; }

		public virtual int SomeFormula { get; set; }

		/// <summary>
		/// Lifecycle method invoked during Save of the entity
		/// </summary>
		protected override void OnSave()
		{
			onSaveCalled = true;
		}

		/// <summary>
		/// Lifecycle method invoked during Update of the entity
		/// </summary>
		protected override void OnUpdate()
		{
			onUpdateCalled = true;
		}

		/// <summary>
		/// Lifecycle method invoked during Delete of the entity
		/// </summary>
		protected override void OnDelete()
		{
			onDeleteCalled = true;
		}

		/// <summary>
		/// Lifecycle method invoked during Load of the entity
		/// </summary>
		protected override void OnLoad(object id)
		{
			onLoadCalled = true;
		}

		public virtual bool OnSaveCalled()
		{
			return onSaveCalled;
		}

		public virtual bool OnUpdateCalled()
		{
			return onUpdateCalled;
		}

		public virtual bool OnDeleteCalled()
		{
			return onDeleteCalled;
		}

		public virtual bool OnLoadCalled()
		{
			return onLoadCalled;
		}

		public virtual ISession CurrentSession {
			get { return (ISession)ActiveRecord<Blog>.Execute((session, blog) => { return session; }, null); }
		}
	}
}
