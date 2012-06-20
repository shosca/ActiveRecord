namespace Castle.ActiveRecord {
	/// <summary>
	/// <para>
	/// Extension point to manipulate mappings
	/// </para>
	/// </summary>
	public interface IMappingContributor {
		void Contribute(NHibernate.Mapping.ByCode.ModelMapper mapper);
	}
}