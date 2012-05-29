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
	public class Company : ActiveRecordBase<Company>
	{
		private int id;
		private String name;
		private IList<Person> _people = new List<Person>();
		private PostalAddress _address;

		public Company()
		{
		}

		public Company(string name)
		{
			this.name = name;
		}

		public int Id
		{
			get { return id; }
			set { id = value; }
		}

		public String Name
		{
			get { return name; }
			set { name = value; }
		}

		public PostalAddress Address
		{
			get { return _address; }
			set { _address = value; }
		}

		public IList<Person> People
		{
			get { return _people; }
			set { _people = value; }
		}
	}

	public class PostalAddress
	{
		private String _address;
		private String _city;
		private String _state;
		private String _zipcode;

		public PostalAddress()
		{
		}

		public PostalAddress(String address, String city,
			String state, String zipcode)
		{
			_address = address;
			_city = city;
			_state = state;
			_zipcode = zipcode;
		}

		public String Address
		{
			get { return _address; }
			set { _address = value; }
		}

		public String City
		{
			get { return _city; }
			set { _city = value;}
		}

		public String State
		{
			get { return _state; }
			set { _state = value; }
		}

		public String ZipCode
		{
			get { return _zipcode; }
			set { _zipcode = value; }
		}
	}
}
