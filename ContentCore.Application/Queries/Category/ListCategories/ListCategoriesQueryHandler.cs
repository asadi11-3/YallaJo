using CategoryEntity = ContentCore.Domain.Entities.Category;
using ContentCore.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Application.Queries.Category.ListCategories;

public sealed class ListCategoriesQueryHandler(ICategoryRepository categoryRepository)
    : IQueryHandler<ListCategoriesQuery, IReadOnlyList<CategoryDto>>
{
    public async Task<Result<IReadOnlyList<CategoryDto>>> Handle(
        ListCategoriesQuery request,
        CancellationToken ct)
    {
        var activeFilter = request.ActiveOnly
            ? (System.Linq.Expressions.Expression<Func<CategoryEntity, bool>>)(c => c.IsActive)
            : null;

        var orderBy = (Func<IQueryable<CategoryEntity>, IOrderedQueryable<CategoryEntity>>)
            (q => q.OrderBy(c => c.SortOrder).ThenBy(c => c.Name));

        // Load categories — include translations when client requested them
        var allCategories = request.WithTranslations
            ? await categoryRepository.GetAllWithTranslationsAsync(
                filter: activeFilter,
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
            .Select(r => BuildNode(r, byParent))
            .ToList() as IReadOnlyList<CategoryDto>;

        return Result<IReadOnlyList<CategoryDto>>.Success(tree);
    }

    private static CategoryDto BuildNode(
        CategoryEntity category,
        ILookup<Guid?, CategoryEntity> byParent)
    {
        var children = byParent[category.Id]
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .Select(c => BuildNode(c, byParent))
            .ToList() as IReadOnlyList<CategoryDto>;

        return CategoryDto.From(category, children);
    }
}
