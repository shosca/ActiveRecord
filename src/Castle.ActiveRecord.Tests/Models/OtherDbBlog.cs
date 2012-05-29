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
	public class OtherDbBlog : Test2ARBase<OtherDbBlog>
	{
		private int _id;
		private String _name;
		private String _author;
		private IList<OtherDbPost> _posts;
		private IList<OtherDbPost> _publishedposts;
		private IList<OtherDbPost> _unpublishedposts;
		private IList<OtherDbPost> _recentposts;

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

		public String Author
		{
			get { return _author; }
			set { _author = value; }
		}

		public IList<OtherDbPost> Posts
		{
			get { return _posts; }
			set { _posts = value; }
		}

		public IList<OtherDbPost> PublishedPosts
		{
			get { return _publishedposts; }
			set { _publishedposts = value; }
		}

		public IList<OtherDbPost> UnPublishedPosts
		{
			get { return _unpublishedposts; }
			set { _unpublishedposts = value; }
		}

		public IList<OtherDbPost> RecentPosts
		{
			get { return _recentposts; }
			set { _recentposts = value; }
		}
	}
}
