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

using System.Collections.Generic;

namespace Castle.ActiveRecord.Tests.Model
{
	//zzzz [ActiveRecord("Hands")]
	public class Hand : Test2ARBase
	{
		private int _id;
		private string _side;

		public Hand()
		{
		}

		//zzzz [PrimaryKey(PrimaryKeyType.Identity)]
		public int Id
		{
			get { return _id; }
			set { _id = value; }
		}

		//zzzz [Property]
		public string Side
		{
			get { return _side; }
			set { _side = value; }
		}

		public static IEnumerable<Hand> FindAll()
		{
			return ActiveRecordMediator<Hand>.FindAll();
		}
	}
}
