using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NHibernate.Mapping.ByCode;

namespace Castle.ActiveRecord.Tests.Models
{
	public class Mapping : IMappingContributor
	{
		public void Contribute(ModelMapper mapper)
		{
			mapper
				.ClassMap<Blog>(map => {
					map.Id(x => x.Id, m => m.Generator(Generators.Native));
					map.Set(x => x.Posts, m =>
					{
						m.Inverse(true);
						m.Lazy(CollectionLazy.Lazy);
					});
					map.Set(x => x.PublishedPosts, m =>
					{
						m.Inverse(true);
						m.Lazy(CollectionLazy.Lazy);
						m.Where("published = 1");
					});
					map.Set(x => x.UnPublishedPosts, m =>
					{
						m.Inverse(true);
						m.Lazy(CollectionLazy.Lazy);
						m.Where("published = 0");
					});
					map.Set(x => x.RecentPosts, m =>
					{
						m.Inverse(true);
						m.Lazy(CollectionLazy.Lazy);
						m.OrderBy(p => p.Created);
					});
				})

				.ClassMap<Post>(map => {
					map.Id(x => x.Id, m => m.Generator(Generators.Native));

				})

				.ClassMap<Company>(map => {
					map.Id(x => x.Id, m => m.Generator(Generators.Native));
				}).ManyToMany<Company, Person>(x => x.People, x => x.Companies)

				.ClassMap<Employee>(map => {
					map.Id(x => x.Id, m => m.Generator(Generators.Native));
					map.OneToOne(x => x.Award, m => m.Constrained(false));
				})

				.ClassMap<Award>(map => {
					map.Id(x => x.Id, m => m.Generator(Generators.Native));
					map.OneToOne(x => x.Employee, m => m.Constrained(true));
				})

				.ClassMap<Person>(map => {
					map.Id(x => x.Id, m => m.Generator(Generators.Native));
					map.Bag(x => x.Companies, m =>
					{
						m.Table("CompanyPerson");
						m.Key(k => k.Column("PersonId"));
					}, m => m.ManyToMany(p =>
					{
						p.Class(typeof(Company));
						p.Column("CompanyId");
					}));
				})

				.ClassMap<Product>(map => {
					map.Id(x => x.Id, m => m.Generator(Generators.Native));
					map.Set(x => x.Categories, m => m.Cascade(Cascade.All | Cascade.DeleteOrphans));
				})

				.ClassMap<Category>(map => {
					map.Id(x => x.Id, m => m.Generator(Generators.Native));
				})


				.ClassMap<Ship>(map => {
					map.Id(x => x.Id, m => m.Generator(Generators.Native));
				})

				.ClassMap<SSAFEntity>(map => {
					map.Id(x => x.Id, m => m.Generator(Generators.GuidComb));
				})
				;

		}
	}
}
