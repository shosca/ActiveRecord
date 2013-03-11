using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace Castle.ActiveRecord.Tests.Models
{
	public class Mapping : DefaultMappingContributor
	{
		public class MapBlog : ClassMapping<Blog> {
			public MapBlog() {
				Id(x => x.Id, m => m.Generator(Generators.Native));
				Set(x => x.Posts, m =>
				{
					m.Inverse(true);
					m.Lazy(CollectionLazy.Lazy);
				});
				Set(x => x.PublishedPosts, m =>
				{
					m.Inverse(true);
					m.Lazy(CollectionLazy.Lazy);
					m.Where("published = 1");
				});
				Set(x => x.UnPublishedPosts, m =>
				{
					m.Inverse(true);
					m.Lazy(CollectionLazy.Lazy);
					m.Where("published = 0");
				});
				Set(x => x.RecentPosts, m =>
				{
					m.Inverse(true);
					m.Lazy(CollectionLazy.Lazy);
					m.OrderBy(p => p.Created);
				});
			}
		}

		public class MapPost : ClassMapping<Post> {
			public MapPost() {
				Id(x => x.Id, m => m.Generator(Generators.Native));
			}
		}

		public class MapCompany : ClassMapping<Company> {
			public MapCompany() {
				Id(x => x.Id, m => m.Generator(Generators.Native));
                Set(x => x.People,
                    cm => {
                        cm.Table("CompanyPeople");
                        cm.Key(k => k.Column("CompanyId"));
                    }
                    ,t => t.ManyToMany(c => c.Column("PersonId")));
			}
		}

		public class MapEmployee : ClassMapping<Employee> {
			public MapEmployee() {
				Id(x => x.Id, m => m.Generator(Generators.Native));
				OneToOne(x => x.Award, m => m.Constrained(false));
			}
		}

		public class MapAward : ClassMapping<Award> {
			public MapAward() {
				Id(x => x.Id, m => m.Generator(Generators.Native));
				OneToOne(x => x.Employee, m => m.Constrained(true));
			}
		}

		public class MapPerson : ClassMapping<Person> {
			public MapPerson() {
				Id(x => x.Id, m => m.Generator(Generators.Native));
                Set(x => x.Companies,
                    cm => {
                        cm.Table("CompanyPeople");
                        cm.Inverse(true);
                        cm.Key(k => k.Column("PersonId"));
                    }
                    ,t => t.ManyToMany(c => c.Column("CompanyId")));
			}
		}
		public class MapProduct : ClassMapping<Product> {
			public MapProduct() {
				Id(x => x.Id, m => m.Generator(Generators.Native));
				Set(x => x.Categories, m => m.Cascade(Cascade.All | Cascade.DeleteOrphans));
			}
		}
		public class MapCategory : ClassMapping<Category> {
			public MapCategory() {
				Id(x => x.Id, m => m.Generator(Generators.Native));
			}
		}

		public class MapShip : ClassMapping<Ship> {
			public MapShip() {
				Id(x => x.Id, m => m.Generator(Generators.Native));
			}
		}

		public class MapSSAFEntity : ClassMapping<SSAFEntity> {
			public MapSSAFEntity() {
				Id(x => x.Id, m => m.Generator(Generators.GuidComb));
			}
		}
	}
}
