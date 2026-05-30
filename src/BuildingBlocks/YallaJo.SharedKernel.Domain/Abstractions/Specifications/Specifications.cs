using System.Linq.Expressions;

namespace YallaJo.SharedKernel.Domain.Abstractions.Specifications
{
    public abstract class Specification<TEntity> : ISpecification<TEntity>
     where TEntity : class
    {
        public Expression<Func<TEntity, bool>>? Criteria { get; private set; }
        public List<Expression<Func<TEntity, bool>>> AdditionalCriteria { get; } = [];

        public List<Expression<Func<TEntity, object>>> IncludeExpressions { get; } = [];
        public List<string> IncludeStrings { get; } = [];
        public List<Func<IQueryable<TEntity>, IQueryable<TEntity>>> IncludeActions { get; } = [];

        public Expression<Func<TEntity, object>>? OrderByExpression { get; private set; }
        public Expression<Func<TEntity, object>>? OrderByDescendingExpression { get; private set; }
        public List<OrderClause<TEntity>> ThenByExpressions { get; } = [];

        public int? Take { get; private set; }
        public int? Skip { get; private set; }
        public bool IsPagingEnabled { get; private set; }

        public Expression<Func<TEntity, object>>? SelectorExpression { get; private set; }
        public Expression<Func<TEntity, object>>? GroupByExpression { get; private set; }

        public bool AsNoTracking { get; private set; } = true;
        public bool AsSplitQuery { get; private set; }
        public bool IgnoreQueryFilters { get; private set; }

        public List<SearchCriteria<TEntity>> SearchCriteriaList { get; } = [];

        public string? CacheKey { get; private set; }
        public TimeSpan? CacheDuration { get; private set; }
        public bool CacheEnabled { get; private set; }

        protected Specification<TEntity> Where(Expression<Func<TEntity, bool>> criteria)
        {
            if (Criteria is null) Criteria = criteria;
            else AdditionalCriteria.Add(criteria);
            return this;
        }

        protected Specification<TEntity> WhereIf(bool condition, Expression<Func<TEntity, bool>> criteria)
        {
            if (condition) Where(criteria);
            return this;
        }

        protected Specification<TEntity> Include(Expression<Func<TEntity, object>> includeExpression)
        {
            IncludeExpressions.Add(includeExpression);
            return this;
        }

        protected Specification<TEntity> Include(string includeString)
        {
            IncludeStrings.Add(includeString);
            return this;
        }

        protected Specification<TEntity> IncludeAction(Func<IQueryable<TEntity>, IQueryable<TEntity>> includeAction)
        {
            IncludeActions.Add(includeAction);
            return this;
        }

        protected Specification<TEntity> OrderBy(Expression<Func<TEntity, object>> orderByExpression)
        {
            OrderByExpression = orderByExpression;
            OrderByDescendingExpression = null;
            return this;
        }

        protected Specification<TEntity> OrderByDescending(Expression<Func<TEntity, object>> orderByDescExpression)
        {
            OrderByDescendingExpression = orderByDescExpression;
            OrderByExpression = null;
            return this;
        }

        protected Specification<TEntity> ThenBy(Expression<Func<TEntity, object>> thenByExpression)
        {
            ThenByExpressions.Add(new OrderClause<TEntity>(thenByExpression, IsDescending: false));
            return this;
        }

        protected Specification<TEntity> ThenByDescending(Expression<Func<TEntity, object>> thenByDescExpression)
        {
            ThenByExpressions.Add(new OrderClause<TEntity>(thenByDescExpression, IsDescending: true));
            return this;
        }

        protected Specification<TEntity> WithPaging(int pageNumber, int pageSize)
        {
            Skip = (pageNumber - 1) * pageSize;
            Take = pageSize;
            IsPagingEnabled = true;
            return this;
        }

        protected Specification<TEntity> WithSkipTake(int skip, int take)
        {
            Skip = skip;
            Take = take;
            IsPagingEnabled = true;
            return this;
        }

        protected Specification<TEntity> WithTop(int count)
        {
            Take = count;
            return this;
        }

        protected Specification<TEntity> Select(Expression<Func<TEntity, object>> selector)
        {
            SelectorExpression = selector;
            return this;
        }

        protected Specification<TEntity> GroupBy(Expression<Func<TEntity, object>> groupByExpression)
        {
            GroupByExpression = groupByExpression;
            return this;
        }

        protected Specification<TEntity> WithTracking()
        {
            AsNoTracking = false;
            return this;
        }

        protected Specification<TEntity> UseSplitQuery()
        {
            AsSplitQuery = true;
            return this;
        }

        protected Specification<TEntity> WithIgnoreQueryFilters()
        {
            IgnoreQueryFilters = true;
            return this;
        }

        protected Specification<TEntity> Search(
            Expression<Func<TEntity, string>> propertySelector,
            string searchTerm,
            int searchGroup = 1)
        {
            SearchCriteriaList.Add(new SearchCriteria<TEntity>(propertySelector, searchTerm, searchGroup));
            return this;
        }

        protected Specification<TEntity> WithCache(string cacheKey, TimeSpan? duration = null)
        {
            CacheKey = cacheKey;
            CacheDuration = duration ?? TimeSpan.FromMinutes(5);
            CacheEnabled = true;
            return this;
        }
    }

    public abstract class Specification<TEntity, TResult> : Specification<TEntity>, ISpecification<TEntity, TResult>
        where TEntity : class
    {
        private Expression<Func<TEntity, TResult>>? _typedSelector;

        public new Expression<Func<TEntity, TResult>>? SelectorExpression => _typedSelector;

        protected Specification<TEntity, TResult> Select(Expression<Func<TEntity, TResult>> selector)
        {
            _typedSelector = selector;
            return this;
        }
    }
}
