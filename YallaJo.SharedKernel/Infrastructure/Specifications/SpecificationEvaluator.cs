using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using YallaJo.SharedKernel.Application.Abstractions.Specifications;

namespace YallaJo.SharedKernel.Infrastructure.Specifications
{
    public static class SpecificationEvaluator
    {
        /// <summary>
        /// Applies all specification criteria to the queryable and returns the resulting query.
        /// </summary>
        public static IQueryable<TEntity> GetQuery<TEntity>(
            IQueryable<TEntity> inputQuery,
            ISpecification<TEntity> specification)
            where TEntity : class
        {
            var query = inputQuery;

            // ─── 1. Ignore Query Filters ─────────────────────────────────
            if (specification.IgnoreQueryFilters)
                query = query.IgnoreQueryFilters();

            // ─── 2. Filtering (Where) ────────────────────────────────────
            if (specification.Criteria is not null)
                query = query.Where(specification.Criteria);

            foreach (var criteria in specification.AdditionalCriteria)
                query = query.Where(criteria);

            // ─── 3. Search (LIKE) ────────────────────────────────────────
            query = ApplySearch(query, specification);

            // ─── 4. Includes (Eager Loading) ─────────────────────────────
            query = specification.IncludeExpressions
                .Aggregate(query, (current, include) => current.Include(include));

            query = specification.IncludeStrings
                .Aggregate(query, (current, include) => current.Include(include));

            foreach (var includeAction in specification.IncludeActions)
                query = includeAction(query);

            // ─── 5. Ordering ─────────────────────────────────────────────
            query = ApplyOrdering(query, specification);

            // ─── 6. Grouping ─────────────────────────────────────────────
            if (specification.GroupByExpression is not null)
            {
                query = query
                    .GroupBy(specification.GroupByExpression)
                    .SelectMany(g => g);
            }

            // ─── 7. Paging ──────────────────────────────────────────────
            if (specification.IsPagingEnabled)
            {
                if (specification.Skip.HasValue && specification.Skip > 0)
                    query = query.Skip(specification.Skip.Value);

                if (specification.Take.HasValue)
                    query = query.Take(specification.Take.Value);
            }
            else if (specification.Take.HasValue)
            {
                // Support WithTop() without full paging
                query = query.Take(specification.Take.Value);
            }

            // ─── 8. Tracking ────────────────────────────────────────────
            if (specification.AsNoTracking)
                query = query.AsNoTracking();

            //if (specification.AsSplitQuery)  // ✅ صح
            //    query = query.AsSplitQuery();

            return query;
        }

        /// <summary>
        /// Applies specification with a typed projection (Select).
        /// </summary>
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

        // ═══════════════════════════════════════════════════════════════════
        //  PRIVATE HELPERS
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Applies search criteria using EF.Functions.Like.
        /// Same group → OR, different groups → AND.
        /// </summary>
        private static IQueryable<TEntity> ApplySearch<TEntity>(
            IQueryable<TEntity> query,
            ISpecification<TEntity> specification)
            where TEntity : class
        {
            if (specification.SearchCriteriaList.Count == 0)
                return query;

            var groups = specification.SearchCriteriaList
                .GroupBy(sc => sc.SearchGroup);

            foreach (var group in groups)
            {
                Expression<Func<TEntity, bool>>? groupExpression = null;

                foreach (var searchCriteria in group)
                {
                    var likePattern = $"%{searchCriteria.SearchTerm}%";

                    // Build: entity => EF.Functions.Like(entity.Property, "%term%")
                    var parameter = searchCriteria.PropertySelector.Parameters[0];
                    var propertyAccess = searchCriteria.PropertySelector.Body;

                    var efFunctionsLike = typeof(DbFunctionsExtensions)
                        .GetMethod(nameof(DbFunctionsExtensions.Like),
                            new[] { typeof(DbFunctions), typeof(string), typeof(string) })!;

                    var likeCall = Expression.Call(
                        efFunctionsLike,
                        Expression.Property(null, typeof(EF), nameof(EF.Functions)),
                        propertyAccess,
                        Expression.Constant(likePattern));

                    var likeLambda = Expression.Lambda<Func<TEntity, bool>>(likeCall, parameter);

                    groupExpression = groupExpression is null
                        ? likeLambda
                        : CombineOr(groupExpression, likeLambda);
                }

                if (groupExpression is not null)
                    query = query.Where(groupExpression);
            }

            return query;
        }

        /// <summary>
        /// Applies ordering (OrderBy, OrderByDescending, ThenBy, ThenByDescending).
        /// </summary>
        private static IQueryable<TEntity> ApplyOrdering<TEntity>(
            IQueryable<TEntity> query,
            ISpecification<TEntity> specification)
            where TEntity : class
        {
            if (specification.OrderByExpression is not null)
            {
                var orderedQuery = query.OrderBy(specification.OrderByExpression);
                query = ApplyThenBy(orderedQuery, specification);
            }
            else if (specification.OrderByDescendingExpression is not null)
            {
                var orderedQuery = query.OrderByDescending(specification.OrderByDescendingExpression);
                query = ApplyThenBy(orderedQuery, specification);
            }

            return query;
        }

        private static IQueryable<TEntity> ApplyThenBy<TEntity>(
            IOrderedQueryable<TEntity> orderedQuery,
            ISpecification<TEntity> specification)
            where TEntity : class
        {
            foreach (var thenBy in specification.ThenByExpressions)
            {
                orderedQuery = thenBy.IsDescending
                    ? orderedQuery.ThenByDescending(thenBy.KeySelector)
                    : orderedQuery.ThenBy(thenBy.KeySelector);
            }

            return orderedQuery;
        }

        /// <summary>
        /// Combines two expressions with OR logic.
        /// </summary>
        private static Expression<Func<T, bool>> CombineOr<T>(
            Expression<Func<T, bool>> left,
            Expression<Func<T, bool>> right)
        {
            var parameter = Expression.Parameter(typeof(T));

            var leftBody = ReplaceParameter(left.Body, left.Parameters[0], parameter);
            var rightBody = ReplaceParameter(right.Body, right.Parameters[0], parameter);

            var combined = Expression.OrElse(leftBody, rightBody);
            return Expression.Lambda<Func<T, bool>>(combined, parameter);
        }

        private static Expression ReplaceParameter(Expression expression, ParameterExpression oldParam, ParameterExpression newParam)
        {
            return new ParameterReplacer(oldParam, newParam).Visit(expression);
        }

        private sealed class ParameterReplacer(ParameterExpression oldParam, ParameterExpression newParam)
            : ExpressionVisitor
        {
            protected override Expression VisitParameter(ParameterExpression node)
                => node == oldParam ? newParam : base.VisitParameter(node);
        }
    }
}
