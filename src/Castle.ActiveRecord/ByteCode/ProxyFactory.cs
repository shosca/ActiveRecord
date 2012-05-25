// Copyright 2004-2011 Castle Project - http://www.castleproject.org/
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using NHibernate.Intercept;

namespace Castle.ActiveRecord.ByteCode 
{
	using System;
	using NHibernate;
	using NHibernate.Engine;
	using NHibernate.Intercept;
	using NHibernate.Proxy;
	using NHibernate.Proxy.DynamicProxy;

    class ProxyFactory : DefaultProxyFactory 
    {
		private readonly NHibernate.Proxy.DynamicProxy.ProxyFactory _factory = new NHibernate.Proxy.DynamicProxy.ProxyFactory();

		public override INHibernateProxy GetProxy(object id, ISessionImplementor session)
		{
			try
			{
				var initializer = new LazyInitializer(EntityName, PersistentClass, id, GetIdentifierMethod, SetIdentifierMethod, ComponentIdType, session);

				object proxyInstance = IsClassProxy
										? _factory.CreateProxy(PersistentClass, initializer, Interfaces)
										: _factory.CreateProxy(Interfaces[0], initializer, Interfaces);

				return (INHibernateProxy)proxyInstance;
			}
			catch (Exception ex)
			{
				log.Error("Creating a proxy instance failed", ex);
				throw new HibernateException("Creating a proxy instance failed", ex);
			}
		}

		public override object GetFieldInterceptionProxy(object instanceToWrap)
		{
			var interceptor = new DefaultDynamicLazyFieldInterceptor(instanceToWrap);
			return _factory.CreateProxy(PersistentClass, interceptor, new[] { typeof(IFieldInterceptorAccessor) });
		}

    }
}
