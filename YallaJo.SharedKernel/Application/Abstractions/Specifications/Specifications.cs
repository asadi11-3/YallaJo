using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace YallaJo.SharedKernel.Application.Abstractions.Specifications
{
    public abstract class Specification<TEntity> : ISpecification<TEntity>
     where TEntity : class
    {
        // ─── Filtering ───────────────────────────────────────────────────
        public Expression<Func<TEntity, bool>>? Criteria { get; private set; }
        public List<Expression<Func<TEntity, bool>>> AdditionalCriteria { get; } = [];

        // ─── Includes ────────────────────────────────────────────────────
        public List<Expression<Func<TEntity, object>>> IncludeExpressions { get; } = [];
        public List<string> IncludeStrings { get; } = [];
        public List<Func<IQueryable<TEntity>, IQueryable<TEntity>>> IncludeActions { get; } = [];

        // ─── Ordering ────────────────────────────────────────────────────
        public Expression<Func<TEntity, object>>? OrderByExpression { get; private set; }
        public Expression<Func<TEntity, object>>? OrderByDescendingExpression { get; private set; }
        public List<OrderClause<TEntity>> ThenByExpressions { get; } = [];

        // ─── Paging ──────────────────────────────────────────────────────
        public int? Take { get; private set; }
        public int? Skip { get; private set; }
        public bool IsPagingEnabled { get; private set; }

        // ─── Projection ──────────────────────────────────────────────────
        public Expression<Func<TEntity, object>>? SelectorExpression { get; private set; }

        // ─── Grouping ────────────────────────────────────────────────────
        public Expression<Func<TEntity, object>>? GroupByExpression { get; private set; }

        // ─── Tracking / Behavior ─────────────────────────────────────────
        public bool AsNoTracking { get; private set; } = true;
        public bool AsSplitQuery { get; private set; }
        public bool IgnoreQueryFilters { get; private set; }

        // ─── Search ──────────────────────────────────────────────────────
        public List<SearchCriteria<TEntity>> SearchCriteriaList { get; } = [];

        // ─── Caching ─────────────────────────────────────────────────────
        public string? CacheKey { get; private set; }
        public TimeSpan? CacheDuration { get; private set; }
        public bool CacheEnabled { get; private set; }

        // ═══════════════════════════════════════════════════════════════════
        //  FLUENT BUILDER METHODS
        // ═══════════════════════════════════════════════════════════════════

        #region Filtering

        /// <summary>
        /// Sets the primary filter criteria. First call sets Criteria; subsequent calls add to AdditionalCriteria (AND logic).
        /// </summary>
        protected Specification<TEntity> Where(Expression<Func<TEntity, bool>> criteria)
        {
            if (Criteria is null)
                Criteria = criteria;
            else
                AdditionalCriteria.Add(criteria);

            return this;
        }

        /// <summary>
        /// Conditionally applies a filter only when the condition is true.
        /// Useful for optional/dynamic filtering.
        /// </summary>
        protected Specification<TEntity> WhereIf(bool condition, Expression<Func<TEntity, bool>> criteria)
        {
            if (condition)
                Where(criteria);

            return this;
        }

        #endregion

        #region Includes

        /// <summary>
        /// Adds a typed include expression for eager loading.
        /// </summary>
        protected Specification<TEntity> Include(Expression<Func<TEntity, object>> includeExpression)
        {
            IncludeExpressions.Add(includeExpression);
            return this;
        }

        /// <summary>
        /// Adds a string-based include path for deep/multi-level eager loading.
        /// </summary>
        protected Specification<TEntity> Include(string includeString)
        {
            IncludeStrings.Add(includeString);
            return this;
        }

        /// <summary>
        /// Adds a custom include action for complex ThenInclude chains.
        /// </summary>
        /// <example>
        /// <code>
        /// IncludeAction(q => q.Include(t => t.Reviews).ThenInclude(r => r.User))
        /// </code>
        /// </example>
        protected Specification<TEntity> IncludeAction(Func<IQueryable<TEntity>, IQueryable<TEntity>> includeAction)
        {
            IncludeActions.Add(includeAction);
            return this;
        }

        #endregion

        #region Ordering

        /// <summary>
        /// Sets ascending order by the specified property.
        /// </summary>
        protected Specification<TEntity> OrderBy(Expression<Func<TEntity, object>> orderByExpression)
        {
            OrderByExpression = orderByExpression;
            OrderByDescendingExpression = null;
            return this;
        }

        /// <summary>
        /// Sets descending order by the specified property.
        /// </summary>
        protected Specification<TEntity> OrderByDescending(Expression<Func<TEntity, object>> orderByDescExpression)
        {
            OrderByDescendingExpression = orderByDescExpression;
            OrderByExpression = null;
            return this;
        }

        /// <summary>
        /// Adds a secondary ascending ordering.
        /// </summary>
        protected Specification<TEntity> ThenBy(Expression<Func<TEntity, object>> thenByExpression)
        {
            ThenByExpressions.Add(new OrderClause<TEntity>(thenByExpression, IsDescending: false));
            return this;
        }

        /// <summary>
        /// Adds a secondary descending ordering.
        /// </summary>
        protected Specification<TEntity> ThenByDescending(Expression<Func<TEntity, object>> thenByDescExpression)
        {
            ThenByExpressions.Add(new OrderClause<TEntity>(thenByDescExpression, IsDescending: true));
            return this;
        }

        #endregion

        #region Paging

        /// <summary>
        /// Enables paging with the specified page number and page size.
        /// </summary>
        protected Specification<TEntity> WithPaging(int pageNumber, int pageSize)
        {
            Skip = (pageNumber - 1) * pageSize;
            Take = pageSize;
            IsPagingEnabled = true;
            return this;
        }

        /// <summary>
        /// Manually sets Skip and Take values.
        /// </summary>
        protected Specification<TEntity> WithSkipTake(int skip, int take)
        {
            Skip = skip;
            Take = take;
            IsPagingEnabled = true;
            return this;
        }

        /// <summary>
        /// Limits the number of results returned.
        /// </summary>
        protected Specification<TEntity> WithTop(int count)
        {
            Take = count;
            return this;
        }

        #endregion

        #region Projection

        /// <summary>
        /// Sets a projection/select expression.
        /// </summary>
        protected Specification<TEntity> Select(Expression<Func<TEntity, object>> selector)
        {
            SelectorExpression = selector;
            return this;
        }

        #endregion

        #region Grouping

        /// <summary>
        /// Sets a group-by expression.
        /// </summary>
        protected Specification<TEntity> GroupBy(Expression<Func<TEntity, object>> groupByExpression)
        {
            GroupByExpression = groupByExpression;
            return this;
        }

        #endregion

        #region Tracking / Behavior

        /// <summary>
        /// Enables change tracking (default is no-tracking).
        /// </summary>
        protected Specification<TEntity> WithTracking()
        {
            AsNoTracking = false;
            return this;
        }

        /// <summary>
        /// Enables split query execution for multi-collection includes.
        /// </summary>
        protected Specification<TEntity> UseSplitQuery()
        {
            AsSplitQuery = true;
            return this;
        }

        /// <summary>
        /// Ignores global query filters (e.g., soft-delete filters).
        /// </summary>
        protected Specification<TEntity> WithIgnoreQueryFilters()
        {
            IgnoreQueryFilters = true;
            return this;
        }

        #endregion

        #region Search

        /// <summary>
        /// Adds a LIKE-based search criteria on a string property.
        /// Multiple search criteria in the same group are combined with OR.
        /// Different groups are combined with AND.
        /// </summary>
        protected Specification<TEntity> Search(
            Expression<Func<TEntity, string>> propertySelector,
            string searchTerm,
            int searchGroup = 1)
        {
            SearchCriteriaList.Add(new SearchCriteria<TEntity>(propertySelector, searchTerm, searchGroup));
            return this;
        }

        #endregion

        #region Caching

        /// <summary>
        /// Enables query result caching with the specified key and optional duration.
        /// </summary>
        protected Specification<TEntity> WithCache(string cacheKey, TimeSpan? duration = null)
        {
            CacheKey = cacheKey;
            CacheDuration = duration ?? TimeSpan.FromMinutes(5);
            CacheEnabled = true;
            return this;
        }

        #endregion
    }

    /// <summary>
    /// Specification with typed projection support.
    /// Use when you need to project to a DTO or anonymous type.
    /// </summary>
    /// <example>
    /// <code>
    /// public class TourSummarySpec : Specification&lt;Tour, TourSummaryDto&gt;
    /// {
    ///     public TourSummarySpec(string city)
    ///     {
    ///         Where(t => t.City == city)
    ///             .Select(t => new TourSummaryDto(t.Id, t.Name, t.Rating));
    ///     }
    /// }
    /// </code>
    /// </example>
    public abstract class Specification<TEntity, TResult> : Specification<TEntity>, ISpecification<TEntity, TResult>
        where TEntity : class
    {
        private Expression<Func<TEntity, TResult>>? _typedSelector;

        public new Expression<Func<TEntity, TResult>>? SelectorExpression => _typedSelector;

        /// <summary>
        /// Sets a typed projection expression.
        /// </summary>
        protected Specification<TEntity, TResult> Select(Expression<Func<TEntity, TResult>> selector)
        {
            _typedSelector = selector;
            return this;
        }
    }
}
