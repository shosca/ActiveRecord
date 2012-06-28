using System.Linq;
using NHibernate.Mapping.ByCode.Conformist;

namespace Castle.ActiveRecord {
	public class DefaultMappingContributor : IMappingContributor {
		public virtual void Contribute(NHibernate.Mapping.ByCode.ModelMapper mapper) {
			var maptypes = GetType().GetNestedTypes().Where(t =>
				t.IsAssignableToGenericType(typeof (ClassMapping<>)) ||
				t.IsAssignableToGenericType(typeof (SubclassMapping<>)) ||
				t.IsAssignableToGenericType(typeof (JoinedSubclassMapping<>)) ||
				t.IsAssignableToGenericType(typeof (UnionSubclassMapping<>))
			).ToArray();
			if (maptypes.Any())
				mapper.AddMappings(maptypes);
		}
	}
}