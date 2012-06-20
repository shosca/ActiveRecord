namespace Castle.ActiveRecord.Tests.Models {
	public class Award : ActiveRecordBase<Award>
	{
		public Award()
		{
		}

		public Award(Employee employee)
		{
			this.Employee = employee;
		}

		public virtual int Id { get; set; }

		public virtual Employee Employee { get; set; }

		public virtual string Description { get; set; }
	}
}