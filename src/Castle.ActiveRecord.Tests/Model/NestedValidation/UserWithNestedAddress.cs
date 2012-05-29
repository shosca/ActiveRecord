namespace Castle.ActiveRecord.Tests.Model.NestedValidation
{
	using Castle.Components.Validator;

	//zzzz [ActiveRecord]
	public class UserWithNestedAddress : ActiveRecordBase<UserWithNestedAddress>
	{
		private int id;
		private string email;
		private Address postalAddress = new Address();
		private Address billingAddress;

		//zzzz [PrimaryKey(PrimaryKeyType.Native)]
		public int Id 
		{
			get { return id; }
			set { id = value; }
		}

		//zzzz [Property]
		[ValidateNonEmpty, ValidateLength(5, 5)]
		public string Email
		{
			get { return email;}
			set { email = value; }
		}

		//zzzz [Nested]
		public Address PostalAddress 
		{
			get { return postalAddress; }
			set { postalAddress = value; }
		}

		//zzzz [Nested]
		public Address BillingAddress {
			get { return billingAddress; }
			set { billingAddress = value; }
		}
	}

	public class Address
	{
		private string addressLine1;
		private string country;

		//zzzz [Property]
		[ValidateNonEmpty, ValidateLength(5,5)]
		public string AddressLine1
		{
			get { return addressLine1; }
			set { addressLine1 = value; }
		}

		//zzzz [Property]
		[ValidateNonEmpty]
		public string Country
		{
			get { return country;}
			set { country = value; }
		}
	}
}