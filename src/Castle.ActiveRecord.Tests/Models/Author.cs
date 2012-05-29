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
using NHibernate;
using NHibernate.Criterion;

namespace Castle.ActiveRecord.Tests.Models
{
	public class Author : UserDB
	{
		private int _id;
		private String _name;

		public Author()
		{
		}

		public Author(int _id)
		{
			this._id = _id;
		}

		public int Id
		{
			get { return _id; }
			set { _id = value; }
		}

		public String Name
		{
			get { return _name; }
			set { _name = value; }
		}

		public ISession CurrentSession {
			get { return (ISession)ActiveRecordMediator<Author>.Execute((session, blog) => { return session; }, null); }
		}
	}
}
