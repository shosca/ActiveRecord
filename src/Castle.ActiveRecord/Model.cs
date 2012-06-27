using System;
using System.Collections.Generic;
using System.Diagnostics;
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
	[DebuggerDisplay("Model: {Type}")]
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
		public ComponentType ComponentType { get; private set; }
		public KeyValuePair<string, IType> PrimaryKey { get; private set; }
		public IDictionary<string, IType> Properties { get; private set; }
		public IDictionary<string, Model> Components { get; private set; }
		public IDictionary<string, ManyToOneType> BelongsTos { get; private set; }
		public IDictionary<string, OneToOneType> OneToOnes { get; private set; }
		public IDictionary<string, AnyType> Anys { get; private set; }
		public IDictionary<string, Collection> HasManys { get; private set; }
		public IDictionary<string, Collection> HasAndBelongsToManys { get; private set; }
		public bool IsComponent { get; private set; }


		public Model(ISessionFactory sessionfactory, ComponentType type) {
			if (type == null) throw new ArgumentNullException("type");

			IsComponent = true;
			Type = type.ReturnedClass;
			ComponentType = type;
			Metadata = sessionfactory.GetClassMetadata(Type);
			Properties = new Dictionary<string, IType>();
			Components = new Dictionary<string, Model>();
			BelongsTos = new Dictionary<string, ManyToOneType>();
			OneToOnes = new Dictionary<string, OneToOneType>();
			Anys = new Dictionary<string, AnyType>();
			HasManys = new Dictionary<string, Collection>();
			HasAndBelongsToManys = new Dictionary<string, Collection>();
			foreach (var pc in type.PropertyNames) {
				var index = type.GetPropertyIndex(pc);
				var x = type.Subtypes[index];
				CategorizeProperty(sessionfactory, x, pc);
			}
			
		}

		public Model(ISessionFactory sessionfactory, Type type)
		{
			if (sessionfactory == null) throw new ArgumentNullException("sessionfactory");
			if (type == null) throw new ArgumentNullException("type");

			IsComponent = false;
			Type = type;
			Metadata = sessionfactory.GetClassMetadata(Type);
			Properties = new Dictionary<string, IType>();
			Components = new Dictionary<string, Model>();
			BelongsTos = new Dictionary<string, ManyToOneType>();
			OneToOnes = new Dictionary<string, OneToOneType>();
			Anys = new Dictionary<string, AnyType>();
			HasManys = new Dictionary<string, Collection>();
			HasAndBelongsToManys = new Dictionary<string, Collection>();

			PrimaryKey = new KeyValuePair<string, IType>(Metadata.IdentifierPropertyName, Metadata.IdentifierType);
			foreach (var name in Metadata.PropertyNames) {
				var prop = Metadata.GetPropertyType(name);
				CategorizeProperty(sessionfactory, prop, name);
			}
			Properties = new ReadOnlyDictionary<string, IType>(Properties);
			Components = new ReadOnlyDictionary<string, Model>(Components);
			BelongsTos = new ReadOnlyDictionary<string, ManyToOneType>(BelongsTos);
			OneToOnes = new ReadOnlyDictionary<string, OneToOneType>(OneToOnes);
			Anys = new ReadOnlyDictionary<string, AnyType>(Anys);
			HasManys = new ReadOnlyDictionary<string, Collection>(HasManys);
			HasAndBelongsToManys = new ReadOnlyDictionary<string, Collection>(HasAndBelongsToManys);
		}

		void CategorizeProperty(ISessionFactory sessionfactory, IType prop, string name) {
			if (prop is ComponentType) {
				Components.Add(name, new Model(sessionfactory, (ComponentType) prop));
			} else if (prop is OneToOneType) {
				OneToOnes.Add(name, (OneToOneType) prop);
			} else if (prop is ManyToOneType) {
				BelongsTos.Add(name, (ManyToOneType) prop);
			} else if (prop is AnyType) {
				Anys.Add(name, (AnyType) prop);
			} else if (prop is CollectionType) {
				var ctype = (CollectionType) prop;
				var persister = sessionfactory.GetCollectionMetadata(ctype.Role) as ICollectionPersister;
				if (persister == null) return;

				if (persister.IsManyToMany) {
					HasAndBelongsToManys.Add(name, new Collection(ctype, persister));
				} else {
					HasManys.Add(name, new Collection(ctype, persister));
				}
			} else {
				Properties.Add(name, prop);
			}
		}
	}
}
