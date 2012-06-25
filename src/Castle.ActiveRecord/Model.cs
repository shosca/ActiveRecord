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
		public class Collection {
			public CollectionType Type { get; private set; }
			public ICollectionPersister Persister { get; private set; }

			public Collection(CollectionType type, ICollectionPersister persister) {
				if (type == null) throw new ArgumentNullException("type");
				if (persister == null) throw new ArgumentNullException("persister");

				Type = type;
				Persister = persister;
			}
		}

		public Type Type { get; private set; }
		public IClassMetadata Metadata { get; private set; }
		public KeyValuePair<string, IType> PrimaryKey { get; private set; }
		public IDictionary<string, IType> Properties { get; private set; }
		public IDictionary<string, ComponentType> Components { get; private set; }
		public IDictionary<string, ManyToOneType> BelongsTos { get; private set; }
		public IDictionary<string, OneToOneType> OneToOnes { get; private set; }
		public IDictionary<string, AnyType> Anys { get; private set; }
		public IDictionary<string, Collection> HasManys { get; private set; }
		public IDictionary<string, Collection> HasAndBelongsToManys { get; private set; }

		public Model(ISessionFactory sessionfactory, Type type)
		{
			if (sessionfactory == null) throw new ArgumentNullException("sessionfactory");
			if (type == null) throw new ArgumentNullException("type");

			Type = type;
			Metadata = sessionfactory.GetClassMetadata(Type);
			var properties = new Dictionary<string, IType>();
			var components = new Dictionary<string, ComponentType>();
			var belongsTos = new Dictionary<string, ManyToOneType>();
			var oneToOnes = new Dictionary<string, OneToOneType>();
			var anys = new Dictionary<string, AnyType>();
			var hasManys = new Dictionary<string, Collection>();
			var hasAndBelongsToManys = new Dictionary<string, Collection>();

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
					belongsTos.Add(name, (ManyToOneType)prop);
				}
				else if (prop is AnyType)
				{
					anys.Add(name, (AnyType)prop);
				}
				else if (prop is CollectionType)
				{
					var ctype = (CollectionType)prop;
					var persister = sessionfactory.GetCollectionMetadata(ctype.Role) as ICollectionPersister;
					if (persister == null) return;

					var reltype = persister.ElementType.ReturnedClass;
					var childmetadata = sessionfactory.GetClassMetadata(reltype);
					if (childmetadata == null) return;

					if (persister.IsManyToMany)
					{
						hasAndBelongsToManys.Add(name, new Collection(ctype, persister));
					}
					else
					{
						hasManys.Add(name, new Collection(ctype, persister));
					}
				}
				else
				{
					properties.Add(name, prop);
				}
			}
			Properties = new ReadOnlyDictionary<string, IType>(properties);
			Components = new ReadOnlyDictionary<string, ComponentType>(components);
			BelongsTos = new ReadOnlyDictionary<string, ManyToOneType>(belongsTos);
			OneToOnes = new ReadOnlyDictionary<string, OneToOneType>(oneToOnes);
			Anys = new ReadOnlyDictionary<string, AnyType>(anys);
			HasManys = new ReadOnlyDictionary<string, Collection>(hasManys);
			HasAndBelongsToManys = new ReadOnlyDictionary<string, Collection>(hasAndBelongsToManys);
		}
	}
}
