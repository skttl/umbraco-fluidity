// <copyright file="DefaultFluidityRepository.cs" company="Matt Brailsford">
// Copyright (c) 2019 Matt Brailsford and contributors.
// Licensed under the Apache License, Version 2.0.
// </copyright>

using System.Collections.Generic;
using System.Linq.Expressions;
using Fluidity.Configuration;
using Fluidity.Events;
using Fluidity.Extensions;
using Fluidity.Models;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Persistence;
using Umbraco.Core.Persistence.SqlSyntax;
using System;
using NPoco;
using SqlExtensions = Fluidity.Extensions.SqlExtensions;
using Umbraco.Core.Scoping;
using Umbraco.Core.Composing;

namespace Fluidity.Data
{
    public class DefaultFluidityRepository : IFluidityRepository
    {
        protected FluidityCollectionConfig _collection;

        protected ISqlSyntaxProvider SyntaxProvider;

        private readonly IScopeProvider _scopeProvider;

        public DefaultFluidityRepository(FluidityCollectionConfig collection)
        {
            _collection = collection;
            _scopeProvider = Current.Factory.GetInstance<IScopeProvider>();
        }

        public Type EntityType => _collection.EntityType;

        public Type IdType => _collection.IdProperty.Type;

        public object Get(object id, bool fireEvents = true)
        {
            using (var scope = _scopeProvider.CreateScope(autoComplete: true))
            {
                var query = new Sql($"SELECT * FROM [{_collection.EntityType.GetTableName()}] WHERE [{_collection.EntityType.GetPrimaryKeyColumnName()}] = '" + id.ToString() + "'");
                return scope.Database.SingleOrDefaultInto(_collection.EntityType, query);
            }
        }

        public IEnumerable<object> GetAll(bool fireEvents = true)
        {
            using (var scope = _scopeProvider.CreateScope(autoComplete: true))
            {
                var query = new Sql($"SELECT * FROM [{_collection.EntityType.GetTableName()}]");

                bool hasWhere = false;
                if (_collection.FilterExpression != null)
                {
                    query.Where(_collection.EntityType, _collection.FilterExpression, scope.Database.SqlContext.SqlSyntax);
                    hasWhere = true;
                }

                if (_collection.DeletedProperty != null)
                {
                    var prefix = !hasWhere ? "WHERE" : "AND";
                    query.Append($" {prefix} {_collection.DeletedProperty.GetColumnName()} = 0 ");
                }

                if (_collection.SortProperty != null)
                {
                    if (_collection.SortDirection == SortDirection.Ascending)
                    {
                        SqlExtensions.OrderBy(query, _collection.EntityType, _collection.SortProperty, scope.Database.SqlContext.SqlSyntax);
                    }
                    else
                    {
                        SqlExtensions.OrderByDescending(query, _collection.EntityType, _collection.SortProperty, scope.Database.SqlContext.SqlSyntax);

                    }
                }

                return scope.Database.Fetch(_collection.EntityType, query);
            }
        }

        public PagedResult<object> GetPaged(int pageNumber, int pageSize, LambdaExpression whereClause, LambdaExpression orderBy, SortDirection orderDirection, bool fireEvents = true)
        {
            using (var scope = _scopeProvider.CreateScope(autoComplete: true))
            {
                var query = new Sql($"SELECT * FROM [{_collection.EntityType.GetTableName()}]");

                // Where
                if (_collection.FilterExpression != null && whereClause != null)
                {
                    var body = Expression.AndAlso(whereClause.Body, _collection.FilterExpression.Body);
                    whereClause = Expression.Lambda(body, whereClause.Parameters[0]);
                }
                else if (_collection.FilterExpression != null)
                {
                    whereClause = _collection.FilterExpression;
                }

                if (whereClause != null)
                {
                    query.Where(_collection.EntityType, whereClause, scope.Database.SqlContext.SqlSyntax);
                }
                else
                {
                    query.Where("1 = 1");
                }

                if (_collection.DeletedProperty != null)
                {
                    query.Append($" AND ({_collection.DeletedProperty.GetColumnName()} = 0)");
                }

                // Order by
                LambdaExpression orderByExp = orderBy ?? _collection.SortProperty;
                if (orderByExp != null)
                {
                    if (orderDirection == SortDirection.Ascending)
                    {
                        SqlExtensions.OrderBy(query, _collection.EntityType, orderByExp, scope.Database.SqlContext.SqlSyntax);
                    }
                    else
                    {
                        SqlExtensions.OrderByDescending(query, _collection.EntityType, orderByExp, scope.Database.SqlContext.SqlSyntax);
                    }
                }
                else
                {
                    // There is a bug in the Db.Page code that effectively requires there
                    // to be an order by clause no matter what, so if one isn't provided
                    // we'lld just order by 1
                    query.Append(" ORDER BY 1 ");
                }

                var result = scope.Database.Page<object>(pageNumber, pageSize, query);

                return new PagedResult<object>(result.TotalItems, pageNumber, pageSize)
                {
                    Items = result.Items
                };
            }
        }

        public object Save(object entity, bool fireEvents = true)
        {
            using (var scope = _scopeProvider.CreateScope(autoComplete: true))
            {
                SavingEntityEventArgs args = null;

                if (fireEvents)
                {
                    var existing = Get(entity.GetPropertyValue(_collection.IdProperty));
                    args = new SavingEntityEventArgs
                    {
                        Entity = new BeforeAndAfter<object>
                        {
                            Before = existing,
                            After = entity
                        }
                    };

                    Fluidity.OnSavingEntity(args);

                    if (args.Cancel)
                        return args.Entity.After;

                    entity = args.Entity.After;
                }

                scope.Database.Save(entity);

                if (fireEvents)
                {
                    Fluidity.OnSavedEntity(args);

                    entity = args.Entity.After;
                }

                return entity;
            }
        }

        public void Delete(object id, bool fireEvents = true)
        {
            using (var scope = _scopeProvider.CreateScope(autoComplete: true))
            {
                DeletingEntityEventArgs args = null;

                if (fireEvents)
                {
                    var existing = Get(id);
                    args = new DeletingEntityEventArgs
                    {
                        Entity = existing
                    };

                    Fluidity.OnDeletingEntity(args);

                    if (args.Cancel)
                        return;

                }

                var query = new Sql(_collection.DeletedProperty != null
                    ? $"UPDATE [{_collection.EntityType.GetTableName()}] SET {_collection.DeletedProperty.GetColumnName()} = 1 WHERE {_collection.IdProperty.GetColumnName()} = @0"
                    : $"DELETE FROM [{_collection.EntityType.GetTableName()}] WHERE {_collection.IdProperty.GetColumnName()} = @0",
                    id);

                scope.Database.Execute(query);

                if (fireEvents)
                    Fluidity.OnDeletedEntity(args);
            }
        }

        public long GetTotalRecordCount(bool fireEvents = true)
        {
            using (var scope = _scopeProvider.CreateScope(autoComplete: true))
            {
                var query = new Sql($"SELECT COUNT(1) FROM [{_collection.EntityType.GetTableName()}]");

                bool hasWhere = false;
                if (_collection.FilterExpression != null)
                {
                    query.Where(_collection.EntityType, _collection.FilterExpression, scope.Database.SqlContext.SqlSyntax);
                    hasWhere = true;
                }

                if (_collection.DeletedProperty != null)
                {
                    var prefix = !hasWhere ? "WHERE" : "AND";
                    query.Append($" {prefix} {_collection.DeletedProperty.GetColumnName()} = 0 ");
                }

                return scope.Database.ExecuteScalar<long>(query);
            }
        }

        public void Dispose()
        {
            //No disposable resources
        }
    }
}