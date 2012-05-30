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
	public class CompanyMapping : ClassMapping<Company> {
		public CompanyMapping() {
			Id(x => x.Id, m => m.Generator(Generators.Native));
		}
	}

	public class Company : ActiveRecordBase<Company>
	{
		public Company() { }

		public Company(string name)
		{
			this.Name = name;
		}

		public virtual int Id { get; set; }

		public virtual string Name { get; set; }

		public virtual PostalAddress Address { get; set; }

		ISet<Person> _people = new HashedSet<Person>();
		public virtual ISet<Person> People
		{
			get { return _people; }
			set { _people = value; }
		}
	}

	public class PostalAddress
	{
		public PostalAddress()
		{
		}

		public PostalAddress(String address, String city,
			String state, String zipcode)
		{
			Address = address;
			City = city;
			State = state;
			ZipCode = zipcode;
		}

		public virtual string Address { get; set; }

		public virtual string City { get; set; }

		public virtual string State { get; set; }

		public virtual string ZipCode { get; set; }
	}
}
