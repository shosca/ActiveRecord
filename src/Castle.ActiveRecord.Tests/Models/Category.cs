using System;

namespace Castle.ActiveRecord.Tests.Models {
	public class Category: ActiveRecordBase<Category> {
		public Category() { }

		public Category(String name) { this.Name = name; }

		public virtual int Id { get; set; }

		public virtual string Name { get; set; }

		public virtual Product Product { get; set; }
	}
}