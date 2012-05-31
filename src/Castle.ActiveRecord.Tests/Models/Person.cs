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
using System.Collections;
using Iesi.Collections.Generic;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace Castle.ActiveRecord.Tests.Models
{
	public class PersonMapping : ClassMapping<Person> {
		public PersonMapping() {
			Id(x => x.Id, m => m.Generator(Generators.Native));
			Bag(x => x.Companies, m => {
				m.Table("CompanyPerson");
				m.Key(k => k.Column("PersonId"));
			}, m => m.ManyToMany(p => {
				p.Class(typeof(Company));
				p.Column("CompanyId");
			}));
		}
	}

	public class Person : ActiveRecordBase<Person>
	{

		public virtual int Id { get; set; }

		public virtual string Name { get; set; }

		public virtual FullName FullName { get; set; }

		public virtual string Address { get; set; }

		public virtual string City { get; set; }

		public virtual Blog Blog { get; set; }

		ISet<Company> _companies = new HashedSet<Company>();
		public virtual ISet<Company> Companies
		{
			get { return _companies; }
			set { _companies = value; }
		}
	}

	public class FullName
	{
		public virtual string First { get; set; }

		public virtual string Middle { get; set; }

		public virtual String Last { get; set; }
	}
}
