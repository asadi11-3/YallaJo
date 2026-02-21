using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YallaJo.SharedKernel.Application.Abstractions.Specifications;

namespace YallaJo.SharedKernel.Infrastructure.Specifications
{
    internal sealed class NoPagingSpecWrapper<TEntity> : ISpecification<TEntity>
    where TEntity : class
    {
        private readonly ISpecification<TEntity> _inner;

        public NoPagingSpecWrapper(ISpecification<TEntity> inner) => _inner = inner;

        public System.Linq.Expressions.Expression<Func<TEntity, bool>>? Criteria => _inner.Criteria;

        public List<System.Linq.Expressions.Expression<Func<TEntity, bool>>> AdditionalCriteria => _inner.AdditionalCriteria;

        public List<System.Linq.Expressions.Expression<Func<TEntity, object>>> IncludeExpressions => _inner.IncludeExpressions;

        public List<string> IncludeStrings => _inner.IncludeStrings;

        public List<Func<IQueryable<TEntity>, IQueryable<TEntity>>> IncludeActions => _inner.IncludeActions;

        public System.Linq.Expressions.Expression<Func<TEntity, object>>? OrderByExpression => _inner.OrderByExpression;

        public System.Linq.Expressions.Expression<Func<TEntity, object>>? OrderByDescendingExpression => _inner.OrderByDescendingExpression;

        public List<OrderClause<TEntity>> ThenByExpressions => _inner.ThenByExpressions;

        public System.Linq.Expressions.Expression<Func<TEntity, object>>? SelectorExpression => _inner.SelectorExpression;

        public System.Linq.Expressions.Expression<Func<TEntity, object>>? GroupByExpression => _inner.GroupByExpression;

        public List<SearchCriteria<TEntity>> SearchCriteriaList => _inner.SearchCriteriaList;

        // ─── Paging DISABLED ──────────────────────────────────────
        public int? Take => null;

        public int? Skip => null;

        public bool IsPagingEnabled => false;

        public bool AsNoTracking => _inner.AsNoTracking;

        public bool AsSplitQuery => _inner.AsSplitQuery;

        public bool IgnoreQueryFilters => _inner.IgnoreQueryFilters;

        public string? CacheKey => null;
        
        public TimeSpan? CacheDuration => null;

        public bool CacheEnabled => false;
    }
}
