using ContentCore.Application.Queries.Category.Common;
using ContentCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using CategoryEntity = ContentCore.Domain.Entities.Category;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Queries.Category.ListCategories;

public sealed class ListCategoriesQueryHandler(
    ICategoryRepository categoryRepository,
    ILogger<ListCategoriesQueryHandler> logger)
    : IQueryHandler<ListCategoriesQuery, IReadOnlyList<CategoryDto>>
{
    public async Task<Result<IReadOnlyList<CategoryDto>>> Handle(
        ListCategoriesQuery request,
        CancellationToken ct)
    {
        try
        {
            var activeFilter = request.ActiveOnly
                ? (System.Linq.Expressions.Expression<Func<CategoryEntity, bool>>)(c => c.IsActive)
                : null;

            var orderBy = (Func<IQueryable<CategoryEntity>, IOrderedQueryable<CategoryEntity>>)
                (q => q.OrderBy(c => c.SortOrder).ThenBy(c => c.Name));

            var allCategories = request.WithTranslations
               ? await categoryRepository.GetAllAsync(
                   filter: activeFilter,
                   include: q => q.Include(c => c.Translations),
                   orderBy: orderBy,
                   ct: ct)
               : await categoryRepository.GetAllAsync(
                   filter: activeFilter,
                   orderBy: q => q.OrderBy(c => c.SortOrder).ThenBy(c => c.Name),
                   ct: ct);

            // Build tree using O(n) lookup — one pass to group, one pass to build
            var byParent = allCategories.ToLookup(c => c.ParentCategoryId);

            IEnumerable<CategoryEntity> roots = request.ParentCategoryId.HasValue
                ? byParent[request.ParentCategoryId]   // return children of the requested parent
                : byParent[null];                        // return top-level roots

            var tree = roots
                .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
                .Select(r => BuildNode(r, byParent, []))
                .ToList() as IReadOnlyList<CategoryDto>;

            logger.LogDebug("ListCategories returned {Count} root nodes", tree.Count);

            return Result<IReadOnlyList<CategoryDto>>.Success(tree);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Result<IReadOnlyList<CategoryDto>>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }

    private static CategoryDto BuildNode(
        CategoryEntity category,
        ILookup<Guid?, CategoryEntity> byParent,
        HashSet<Guid> visited)
    {
        // Cycle detection: if we've already visited this node, return it as a leaf.
        // This handles corrupt DB data with circular parent references and prevents StackOverflow.
        if (!visited.Add(category.Id))
            return CategoryDto.From(category, []);

        var children = byParent[category.Id]
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .Select(c => BuildNode(c, byParent, visited))
            .ToList() as IReadOnlyList<CategoryDto>;

        return CategoryDto.From(category, children);
    }
}
