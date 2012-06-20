using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using NHibernate.Mapping.ByCode;

namespace Castle.ActiveRecord {
	public static class Conventions
	{
		public static ModelMapper ClassMap<TRootEntity>(this ModelMapper mapper, Action<IClassMapper<TRootEntity>> mapaction) where TRootEntity : class {
			mapper.Class(mapaction);
			return mapper;
		}

		public static ModelMapper SubclassMap<TEntity>(this ModelMapper mapper, Action<ISubclassMapper<TEntity>> mapaction) where TEntity : class {
			mapper.Subclass(mapaction);
			return mapper;
		}
		public static ModelMapper UnionSubclassMap<TEntity>(this ModelMapper mapper, Action<IUnionSubclassMapper<TEntity>> mapaction) where TEntity : class {
			mapper.UnionSubclass(mapaction);
			return mapper;
		}

		public static ModelMapper ManyToMany<TControllingEntity, TInverseEntity>
		(
			this ModelMapper mapper,
			Expression<Func<TControllingEntity, IEnumerable<TInverseEntity>>> controllingProperty,
			Expression<Func<TInverseEntity, IEnumerable<TControllingEntity>>> inverseProperty
		) where TControllingEntity : class where TInverseEntity : class {
			return mapper.ManyToMany(controllingProperty, inverseProperty, "{0}_key", "{0}_{1}");
		}

		public static ModelMapper ManyToMany<TControllingEntity, TInverseEntity>
		(
			this ModelMapper mapper,
			Expression<Func<TControllingEntity, IEnumerable<TInverseEntity>>> controllingProperty,
			string columnformat, string tableformat
		) where TControllingEntity : class where TInverseEntity : class
		{
			return mapper.ManyToMany(controllingProperty, null, columnformat, tableformat);
		}

		public static ModelMapper ManyToMany<TControllingEntity, TInverseEntity>
		(
			this ModelMapper mapper,
			Expression<Func<TControllingEntity, IEnumerable<TInverseEntity>>> controllingProperty
		) where TControllingEntity : class where TInverseEntity : class {
			return mapper.ManyToMany(controllingProperty, "{0}_key", "{0}_{1}");
		}

		public static ModelMapper ManyToMany<TControllingEntity, TInverseEntity>
		(
			this ModelMapper mapper,
			Expression<Func<TControllingEntity, IEnumerable<TInverseEntity>>> controllingProperty,
			Expression<Func<TInverseEntity, IEnumerable<TControllingEntity>>> inverseProperty,
			string columnformat, string tableformat
		) where TControllingEntity : class where TInverseEntity : class
		{
			var controllingPropertyName = ((MemberExpression)controllingProperty.Body).Member.Name;
			var controllingColumnName = string.Format(columnformat, controllingPropertyName);
			var inverseColumnName = string.Format(columnformat, typeof(TControllingEntity).Name);
			var tableName = string.Format(tableformat, typeof(TControllingEntity).Name, controllingPropertyName);

			mapper.Class<TControllingEntity>(map => map.Set(controllingProperty,
				cm =>
				{
					cm.Cascade(Cascade.Persist | Cascade.Remove);
					cm.Table(tableName);
					cm.Key(km => km.Column(controllingColumnName));
				},
				em => em.ManyToMany(m => m.Column(inverseColumnName))));

			if (inverseProperty != null)
				mapper.Class<TInverseEntity>(map => map.Set(inverseProperty,
					cm =>
					{
						cm.Table(tableName);
						cm.Inverse(true);
						cm.Key(km => km.Column(inverseColumnName));
					},
					em => em.ManyToMany(m => m.Column(controllingColumnName))));

			return mapper;
		}

	}
}