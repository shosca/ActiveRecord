using System;

namespace Castle.ActiveRecord.Tests.Models {
	public class PostalAddress
	{
		public PostalAddress()
		{
		}

		public PostalAddress(String address, String city,
		                     String state, String zipcode)
		{
			Address = address;
			City = city;
			State = state;
			ZipCode = zipcode;
		}

		public virtual string Address { get; set; }

		public virtual string City { get; set; }

		public virtual string State { get; set; }

		public virtual string ZipCode { get; set; }
	}
}