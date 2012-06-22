using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace Castle.ActiveRecord.Tests.Model
{
	public class Mapping : DefaultMappingContributor
	{
		public class MapARClass : ClassMapping<ActiveRecordClass> {
			public MapARClass() {
				Id(x => x.Id, m => m.Generator(Generators.Native));
			}
		}

		public class MapAuthor : ClassMapping<Author> {
			public MapAuthor() {
				Id(x => x.Id, m => m.Generator(Generators.Native));
			}
		}

		public class MapHand : ClassMapping<Hand> {
			public MapHand() {
				Id(x => x.Id, m => m.Generator(Generators.Identity));
			}
		}

		public class MapOtherDbBlog : ClassMapping<OtherDbBlog> {
			public MapOtherDbBlog() {
				Table("Blog");
				Id(x => x.Id, m => m.Generator(Generators.Native));
			}
		}

		public class MapOtherDbPost : ClassMapping<OtherDbPost> {
			public MapOtherDbPost() {
				Table("Post");
				Id(x => x.Id, m => m.Generator(Generators.Native));
			}
		}
	}
}
