using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace Castle.ActiveRecord.Tests.Model
{
	public class Mapping : IMappingContributor
	{
		public void Contribute(ModelMapper mapper) {
			mapper
				.ClassMap<ActiveRecordClass>(map => {
					map.Id(x => x.Id, m => m.Generator(Generators.Native));
				})

				.ClassMap<Author>(map => {
					map.Id(x => x.Id, m => m.Generator(Generators.Native));
				})

				.ClassMap<Hand>(map => {
					map.Id(x => x.Id, m => m.Generator(Generators.Identity));
				})

				.ClassMap<OtherDbBlog>(map => {
					map.Table("Blog");
					map.Id(x => x.Id, m => m.Generator(Generators.Native));
				})

				.ClassMap<OtherDbPost>(map => {
					map.Table("Post");
					map.Id(x => x.Id, m => m.Generator(Generators.Native));
				});
		}
	}
}
