using System;
using System.Collections.Generic;
using System.Linq;
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

		public static ModelMapper ManyToManyBag<TControllingEntity, TInverseEntity>
		(
			this ModelMapper mapper,
			Expression<Func<TControllingEntity, IEnumerable<TInverseEntity>>> controllingProperty,
			Expression<Func<TInverseEntity, IEnumerable<TControllingEntity>>> inverseProperty
		) where TControllingEntity : class where TInverseEntity : class {
			return mapper.ManyToManyBag(controllingProperty, inverseProperty, "{0}_key", "{0}_{1}");
		}

		public static ModelMapper ManyToManyBag<TControllingEntity, TInverseEntity>
		(
			this ModelMapper mapper,
			Expression<Func<TControllingEntity, IEnumerable<TInverseEntity>>> controllingProperty,
			string columnformat, string tableformat
		) where TControllingEntity : class where TInverseEntity : class
		{
			return mapper.ManyToManyBag(controllingProperty, null, columnformat, tableformat);
		}

		public static ModelMapper ManyToManyBag<TControllingEntity, TInverseEntity>
		(
			this ModelMapper mapper,
			Expression<Func<TControllingEntity, IEnumerable<TInverseEntity>>> controllingProperty
		) where TControllingEntity : class where TInverseEntity : class {
			return mapper.ManyToManyBag(controllingProperty, "{0}_key", "{0}_{1}");
		}

		public static ModelMapper ManyToManyBag<TControllingEntity, TInverseEntity>
		(
			this ModelMapper mapper,
			Expression<Func<TControllingEntity, IEnumerable<TInverseEntity>>> controllingProperty,
			Expression<Func<TInverseEntity, IEnumerable<TControllingEntity>>> inverseProperty,
			string columnformat, string tableformat
		) where TControllingEntity : class where TInverseEntity : class
		{
			var controllingPropertyName = ((MemberExpression)controllingProperty.Body).Member.Name.ToLowerInvariant();
			var controllingColumnName = string.Format(columnformat, controllingPropertyName).ToLowerInvariant();
			var inverseColumnName = string.Format(columnformat, typeof(TControllingEntity).Name).ToLowerInvariant();
			var tableName = string.Format(tableformat, typeof (TControllingEntity).Name, controllingPropertyName).ToLowerInvariant();
			mapper.Class<TControllingEntity>(
				map => map.Bag(controllingProperty,
					cm => {
						cm.Table(tableName);
						cm.Key(km => km.Column(inverseColumnName));
					},
					em => em.ManyToMany(m => m.Column(controllingColumnName))
				)
			);
			if (inverseProperty != null)
				mapper.Class<TInverseEntity>(
					map => map.Bag(inverseProperty,
						cm => {
							cm.Table(tableName);
							cm.Inverse(true);
							cm.Key(km => km.Column(controllingColumnName));
						},
						em => em.ManyToMany(m => m.Column(inverseColumnName))
					)
				);

			return mapper;
		}

		public static ModelMapper ManyToManySet<TControllingEntity, TInverseEntity>
		(
			this ModelMapper mapper,
			Expression<Func<TControllingEntity, IEnumerable<TInverseEntity>>> controllingProperty,
			Expression<Func<TInverseEntity, IEnumerable<TControllingEntity>>> inverseProperty
		) where TControllingEntity : class where TInverseEntity : class {
			return mapper.ManyToManySet(controllingProperty, inverseProperty, "{0}_key", "{0}_{1}");
		}

		public static ModelMapper ManyToManySet<TControllingEntity, TInverseEntity>
		(
			this ModelMapper mapper,
			Expression<Func<TControllingEntity, IEnumerable<TInverseEntity>>> controllingProperty,
			string columnformat, string tableformat
		) where TControllingEntity : class where TInverseEntity : class
		{
			return mapper.ManyToManySet(controllingProperty, null, columnformat, tableformat);
		}

		public static ModelMapper ManyToManySet<TControllingEntity, TInverseEntity>
		(
			this ModelMapper mapper,
			Expression<Func<TControllingEntity, IEnumerable<TInverseEntity>>> controllingProperty
		) where TControllingEntity : class where TInverseEntity : class {
			return mapper.ManyToManySet(controllingProperty, "{0}_key", "{0}_{1}");
		}

		public static ModelMapper ManyToManySet<TControllingEntity, TInverseEntity>
		(
			this ModelMapper mapper,
			Expression<Func<TControllingEntity, IEnumerable<TInverseEntity>>> controllingProperty,
			Expression<Func<TInverseEntity, IEnumerable<TControllingEntity>>> inverseProperty,
			string columnformat, string tableformat
		) where TControllingEntity : class where TInverseEntity : class
		{
			var controllingPropertyName = ((MemberExpression)controllingProperty.Body).Member.Name.ToLowerInvariant();
			var controllingColumnName = string.Format(columnformat, controllingPropertyName).ToLowerInvariant();
			var inverseColumnName = string.Format(columnformat, typeof(TControllingEntity).Name).ToLowerInvariant();
			var tableName = string.Format(tableformat, typeof (TControllingEntity).Name, controllingPropertyName).ToLowerInvariant();
			mapper.Class<TControllingEntity>(
				map => map.Set(controllingProperty,
					cm => {
						cm.Table(tableName);
						cm.Key(km => km.Column(inverseColumnName));
					},
					em => em.ManyToMany(m => m.Column(controllingColumnName))
				)
			);
			if (inverseProperty != null)
				mapper.Class<TInverseEntity>(
					map => map.Set(inverseProperty,
						cm => {
							cm.Table(tableName);
							cm.Inverse(true);
							cm.Key(km => km.Column(controllingColumnName));
						},
						em => em.ManyToMany(m => m.Column(inverseColumnName))
					)
				);

			return mapper;
		}


		/// <summary>
		/// Determines whether the <paramref name="genericType"/> is assignable from
		/// <paramref name="givenType"/> taking into account generic definitions
		/// </summary>
		public static bool IsAssignableToGenericType(this Type givenType, Type genericType)
		{
			var interfaceTypes = givenType.GetInterfaces();

			foreach (var it in interfaceTypes)
				if (it.IsGenericType)
					if (it.GetGenericTypeDefinition() == genericType) return true;

			Type baseType = givenType.BaseType;
			if (baseType == null) return false;

			return baseType.IsGenericType &&
				baseType.GetGenericTypeDefinition() == genericType ||
				IsAssignableToGenericType(baseType, genericType);
		}
	}
}
