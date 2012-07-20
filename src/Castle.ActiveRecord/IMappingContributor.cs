namespace Castle.ActiveRecord {
	/// <summary>
	/// Extension point to manipulate mappings
	/// </summary>
	public interface IMappingContributor {
		void Contribute(NHibernate.Mapping.ByCode.ModelMapper mapper);
	}
}