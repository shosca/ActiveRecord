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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Castle.Core.Internal;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Impl;

namespace Castle.ActiveRecord {
    public static class QueryExtensions {
        #region DetachedCriteria

        public static DetachedCriteria AddOrders(this DetachedCriteria criteria, params Order[] orders) {
            orders.ForEach(o => criteria.AddOrder(o));
            return criteria;
        }

        public static DetachedCriteria AddCriterias(this DetachedCriteria criteria, params ICriterion[] criterias) {
            criterias.ForEach(o => criteria.Add(o));
            return criteria;
        }

        public static DetachedCriteria AddOrder<T>(this DetachedCriteria criteria, Expression<Func<T, object>> expression) {
            return criteria.AddOrder(expression, true);
        }

        public static DetachedCriteria AddOrder<T>(this DetachedCriteria criteria, Expression<Func<T, object>> expression, bool asc) {
            return
                criteria.AddOrder(asc
                                      ? Order.Asc(Projections.Property(expression))
                                      : Order.Desc(Projections.Property(expression)));
        }

        public static ICriteria AddOrder<T>(this ICriteria criteria, Expression<Func<T, object>> expression) {
            return criteria.AddOrder(expression, true);
        }

        public static ICriteria AddOrder<T>(this ICriteria criteria, Expression<Func<T, object>> expression, bool asc) {
            return criteria.AddOrder(asc ?
                                         Order.Asc(Projections.Property(expression))
                                         : Order.Desc(Projections.Property(expression)));
        }

        #endregion
    }
}
