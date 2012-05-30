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

using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace Castle.ActiveRecord.Tests.Models
{
	public class EmployeeMapping : ClassMapping<Employee> {
		public EmployeeMapping() {
			Id(x => x.Id, m => m.Generator(Generators.Native));
		}
	}

	public class AwardMapping : ClassMapping<Award> {
		public AwardMapping() {
			Id(x => x.Id, m => m.Generator(Generators.Native));
		}
	}

	public class Employee : ActiveRecordBase<Employee>
	{
		public virtual int Id { get; set; }

		public virtual string FirstName { get; set; }

		public virtual string LastName { get; set; }

		public virtual Award Award { get; set; }
	}

	public class Award : ActiveRecordBase<Employee>
	{
		public Award()
		{
		}

		public Award(Employee employee)
		{
			this.Employee = employee;
		}

		public virtual int Id { get; set; }

		public virtual Employee Employee { get; set; }

		public virtual string Description { get; set; }
	}
}
