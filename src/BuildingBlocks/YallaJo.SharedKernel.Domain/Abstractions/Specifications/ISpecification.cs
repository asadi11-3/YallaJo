using System.Linq.Expressions;

namespace YallaJo.SharedKernel.Domain.Abstractions.Specifications
{
    public interface ISpecification<TEntity> where TEntity : class
    {
        Expression<Func<TEntity, bool>>? Criteria { get; }
        List<Expression<Func<TEntity, bool>>> AdditionalCriteria { get; }

        List<Expression<Func<TEntity, object>>> IncludeExpressions { get; }
        List<string> IncludeStrings { get; }
        List<Func<IQueryable<TEntity>, IQueryable<TEntity>>> IncludeActions { get; }

        Expression<Func<TEntity, object>>? OrderByExpression { get; }
        Expression<Func<TEntity, object>>? OrderByDescendingExpression { get; }
        List<OrderClause<TEntity>> ThenByExpressions { get; }

        int? Take { get; }
        int? Skip { get; }
        bool IsPagingEnabled { get; }

        Expression<Func<TEntity, object>>? SelectorExpression { get; }
        Expression<Func<TEntity, object>>? GroupByExpression { get; }

        bool AsNoTracking { get; }
        bool AsSplitQuery { get; }
        bool IgnoreQueryFilters { get; }

        List<SearchCriteria<TEntity>> SearchCriteriaList { get; }

        string? CacheKey { get; }
        TimeSpan? CacheDuration { get; }
        bool CacheEnabled { get; }
    }

    public interface ISpecification<TEntity, TResult> : ISpecification<TEntity>
        where TEntity : class
    {
        new Expression<Func<TEntity, TResult>>? SelectorExpression { get; }
    }

    public sealed record OrderClause<TEntity>(
        Expression<Func<TEntity, object>> KeySelector,
        bool IsDescending
    );

    public sealed record SearchCriteria<TEntity>(
        Expression<Func<TEntity, string>> PropertySelector,
        string SearchTerm,
        int SearchGroup = 1
    );
}
