using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace YallaJo.SharedKernel.Application.Abstractions.Specifications
{
    public interface ISpecification<TEntity> where TEntity : class
    {
        // ─── Filtering ───────────────────────────────────────────────────
        Expression<Func<TEntity, bool>>? Criteria { get; }
        List<Expression<Func<TEntity, bool>>> AdditionalCriteria { get; }

        // ─── Includes (Eager Loading) ────────────────────────────────────
        List<Expression<Func<TEntity, object>>> IncludeExpressions { get; }
        List<string> IncludeStrings { get; }
        List<Func<IQueryable<TEntity>, IQueryable<TEntity>>> IncludeActions { get; }

        // ─── Ordering ────────────────────────────────────────────────────
        Expression<Func<TEntity, object>>? OrderByExpression { get; }
        Expression<Func<TEntity, object>>? OrderByDescendingExpression { get; }
        List<OrderClause<TEntity>> ThenByExpressions { get; }

        // Paging 
        int? Take { get; }
        int? Skip { get; }
        bool IsPagingEnabled { get; }

        // Projection
        Expression<Func<TEntity, object>>? SelectorExpression { get; }

        // Grouping
        Expression<Func<TEntity, object>>? GroupByExpression { get; }

        // Tracking
        bool AsNoTracking { get; }
        bool AsSplitQuery { get; }
        bool IgnoreQueryFilters { get; }

        // Search 
        List<SearchCriteria<TEntity>> SearchCriteriaList { get; }

        // Caching 
        string? CacheKey { get; }
        TimeSpan? CacheDuration { get; }
        bool CacheEnabled { get; }
    }

    /// <summary>
    /// Extends ISpecification with a typed projection result.
    /// </summary>
    public interface ISpecification<TEntity, TResult> : ISpecification<TEntity>
        where TEntity : class
    {
        new Expression<Func<TEntity, TResult>>? SelectorExpression { get; }
    }

    /// <summary>
    /// Represents an ordering clause (ThenBy / ThenByDescending).
    /// </summary>
    public sealed record OrderClause<TEntity>(
        Expression<Func<TEntity, object>> KeySelector,
        bool IsDescending
    );

    /// <summary>
    /// Represents a search criteria for LIKE-based filtering.
    /// </summary>
    public sealed record SearchCriteria<TEntity>(
        Expression<Func<TEntity, string>> PropertySelector,
        string SearchTerm,
        int SearchGroup = 1
    );
}
