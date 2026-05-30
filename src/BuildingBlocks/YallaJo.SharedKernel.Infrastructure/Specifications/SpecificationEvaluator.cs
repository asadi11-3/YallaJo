using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using YallaJo.SharedKernel.Domain.Abstractions.Specifications;

namespace YallaJo.SharedKernel.Infrastructure.Specifications
{
    public static class SpecificationEvaluator
    {
        public static IQueryable<TEntity> GetQuery<TEntity>(
            IQueryable<TEntity> inputQuery,
            ISpecification<TEntity> specification,
            bool evaluatePaging = true)
            where TEntity : class
        {
            var query = inputQuery;

            if (specification.IgnoreQueryFilters)
                query = query.IgnoreQueryFilters();

            if (specification.Criteria is not null)
                query = query.Where(specification.Criteria);

            foreach (var criteria in specification.AdditionalCriteria)
                query = query.Where(criteria);

            query = ApplySearch(query, specification);

            query = specification.IncludeExpressions
                .Aggregate(query, (current, include) => current.Include(include));
            query = specification.IncludeStrings
                .Aggregate(query, (current, include) => current.Include(include));
            foreach (var includeAction in specification.IncludeActions)
                query = includeAction(query);

            query = ApplyOrdering(query, specification);

            if (specification.GroupByExpression is not null)
                query = query.GroupBy(specification.GroupByExpression).SelectMany(g => g);

            if (evaluatePaging && specification.IsPagingEnabled)
            {
                if (specification.Skip.HasValue && specification.Skip > 0)
                    query = query.Skip(specification.Skip.Value);
                if (specification.Take.HasValue)
                    query = query.Take(specification.Take.Value);
            }
            else if (evaluatePaging && specification.Take.HasValue)
            {
                query = query.Take(specification.Take.Value);
            }

            if (specification.AsNoTracking) query = query.AsNoTracking();
            if (specification.AsSplitQuery) query = query.AsSplitQuery();

            return query;
        }

        public static IQueryable<TResult> GetQuery<TEntity, TResult>(
            IQueryable<TEntity> inputQuery,
            ISpecification<TEntity, TResult> specification)
            where TEntity : class
        {
            var query = GetQuery(inputQuery, (ISpecification<TEntity>)specification);

            if (specification.SelectorExpression is not null)
                return query.Select(specification.SelectorExpression);

            throw new InvalidOperationException(
                $"Specification<{typeof(TEntity).Name}, {typeof(TResult).Name}> must have a SelectorExpression defined.");
        }

        private static IQueryable<TEntity> ApplySearch<TEntity>(IQueryable<TEntity> query, ISpecification<TEntity> specification)
            where TEntity : class
        {
            if (specification.SearchCriteriaList.Count == 0) return query;

            var groups = specification.SearchCriteriaList.GroupBy(sc => sc.SearchGroup);
            foreach (var group in groups)
            {
                Expression<Func<TEntity, bool>>? groupExpression = null;
                foreach (var searchCriteria in group)
                {
                    var likePattern = $"%{searchCriteria.SearchTerm}%";
                    var parameter = searchCriteria.PropertySelector.Parameters[0];
                    var propertyAccess = searchCriteria.PropertySelector.Body;

                    var efFunctionsLike = typeof(DbFunctionsExtensions)
                        .GetMethod(nameof(DbFunctionsExtensions.Like),
                            new[] { typeof(DbFunctions), typeof(string), typeof(string) })!;

                    var likeCall = Expression.Call(efFunctionsLike,
                        Expression.Property(null, typeof(EF), nameof(EF.Functions)),
                        propertyAccess, Expression.Constant(likePattern));

                    var likeLambda = Expression.Lambda<Func<TEntity, bool>>(likeCall, parameter);
                    groupExpression = groupExpression is null ? likeLambda : CombineOr(groupExpression, likeLambda);
                }
                if (groupExpression is not null) query = query.Where(groupExpression);
            }
            return query;
        }

        private static IQueryable<TEntity> ApplyOrdering<TEntity>(IQueryable<TEntity> query, ISpecification<TEntity> specification)
            where TEntity : class
        {
            if (specification.OrderByExpression is not null)
                return ApplyThenBy(query.OrderBy(specification.OrderByExpression), specification);
            if (specification.OrderByDescendingExpression is not null)
                return ApplyThenBy(query.OrderByDescending(specification.OrderByDescendingExpression), specification);
            return query;
        }

        private static IQueryable<TEntity> ApplyThenBy<TEntity>(IOrderedQueryable<TEntity> orderedQuery, ISpecification<TEntity> specification)
            where TEntity : class
        {
            foreach (var thenBy in specification.ThenByExpressions)
                orderedQuery = thenBy.IsDescending
                    ? orderedQuery.ThenByDescending(thenBy.KeySelector)
                    : orderedQuery.ThenBy(thenBy.KeySelector);
            return orderedQuery;
        }

        private static Expression<Func<T, bool>> CombineOr<T>(Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
        {
            var parameter = Expression.Parameter(typeof(T));
            var leftBody = ReplaceParameter(left.Body, left.Parameters[0], parameter);
            var rightBody = ReplaceParameter(right.Body, right.Parameters[0], parameter);
            return Expression.Lambda<Func<T, bool>>(Expression.OrElse(leftBody, rightBody), parameter);
        }

        private static Expression ReplaceParameter(Expression expression, ParameterExpression oldParam, ParameterExpression newParam)
            => new ParameterReplacer(oldParam, newParam).Visit(expression);

        private sealed class ParameterReplacer(ParameterExpression oldParam, ParameterExpression newParam) : ExpressionVisitor
        {
            protected override Expression VisitParameter(ParameterExpression node)
                => node == oldParam ? newParam : base.VisitParameter(node);
        }
    }
}
