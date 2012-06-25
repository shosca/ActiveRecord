using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NHibernate;
using NHibernate.Metadata;
using NHibernate.Persister.Collection;
using NHibernate.Type;

namespace Castle.ActiveRecord
{
	/// <summary>
	/// An easier to work with wrapper for IClassMetadata
	/// </summary>
	public class Model
	{
		public IClassMetadata Metadata { get; private set; }
		public KeyValuePair<string, IType> PrimaryKey { get; private set; }
		public IDictionary<string, IType> Properties { get; private set; }
		public IDictionary<string, ComponentType> Components { get; private set; }
		public IDictionary<string, EntityType> BelongsTos { get; private set; }
		public IDictionary<string, OneToOneType> OneToOnes { get; private set; }
		public IDictionary<string, CollectionType> HasManys { get; private set; }
		public IDictionary<string, CollectionType> HasAndBelongsToManys { get; private set; }

		public Model(ISessionFactory sessionfactory, Type type)
		{
			Metadata = sessionfactory.GetClassMetadata(type);
			var properties = new Dictionary<string, IType>();
			var components = new Dictionary<string, ComponentType>();
			var belongsTos = new Dictionary<string, EntityType>();
			var oneToOnes = new Dictionary<string, OneToOneType>();
			var hasManys = new Dictionary<string, CollectionType>();
			var hasAndBelongsToManys = new Dictionary<string, CollectionType>();

			PrimaryKey = new KeyValuePair<string, IType>(Metadata.IdentifierPropertyName, Metadata.IdentifierType);
			foreach (var name in Metadata.PropertyNames)
			{
				var prop = Metadata.GetPropertyType(name);
				if (prop is ComponentType)
				{
					components.Add(name, (ComponentType)prop);
				}
				else if (prop is OneToOneType)
				{
					oneToOnes.Add(name, (OneToOneType)prop);
				}
				else if (prop is ManyToOneType)
				{
					belongsTos.Add(name, (EntityType)prop);
				}
				else if (prop is CollectionType)
				{
					var ctype = (CollectionType)prop;
					var reltype = ctype.ReturnedClass.GetGenericArguments().FirstOrDefault();
					var childmetadata = sessionfactory.GetClassMetadata(reltype);
					if (childmetadata == null) return;

					var persister = sessionfactory.GetCollectionMetadata(ctype.Role) as ICollectionPersister;
					if (persister == null) return;
					if (persister.IsOneToMany)
					{
						hasManys.Add(name, ctype);
					}
					else if (persister.IsManyToMany)
					{
						hasAndBelongsToManys.Add(name, ctype);
					}
				}
				else
				{
					properties.Add(name, prop);
				}
			}
			Properties = new ReadOnlyDictionary<string, IType>(properties);
			Components = new ReadOnlyDictionary<string, ComponentType>(components);
			BelongsTos = new ReadOnlyDictionary<string, EntityType>(belongsTos);
			OneToOnes = new ReadOnlyDictionary<string, OneToOneType>(oneToOnes);
			HasManys = new ReadOnlyDictionary<string, CollectionType>(hasManys);
			HasAndBelongsToManys = new ReadOnlyDictionary<string, CollectionType>(hasAndBelongsToManys);
		}
	}
}
