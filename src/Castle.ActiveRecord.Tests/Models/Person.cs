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
using System.Collections.Generic;

namespace Castle.ActiveRecord.Tests.Models
{
	public class Person : ActiveRecordBase<Person>
	{
		private int _id;
		private String _name;
		private FullName _fullName;
		private String _address;
		private IList<Company> _companies = new List<Company>();
		private Blog _blog;

		public int Id
		{
			get { return _id; }
			set { _id = value; }
		}

		public string Name
		{
			get { return _name; }
			set { _name = value; }
		}

		public FullName FullName
		{
			get { return _fullName; }
			set { _fullName = value; }
		}

		public string Address
		{
			get { return _address; }
			set { _address = value; }
		}

		public string City;

		public Blog Blog
		{
			get { return _blog; }
			set { _blog = value; }
		}

		public IList<Company> Companies
		{
			get { return _companies; }
			set { _companies = value; }
		}
	}

	public class FullName
	{
		private String _first;
		private String _middle;

		public String First
		{
			get { return _first; }
			set { _first = value; }
		}

		public String Middle
		{
			get { return _middle; }
			set { _middle = value; }
		}

		public String Last;
	}
}
