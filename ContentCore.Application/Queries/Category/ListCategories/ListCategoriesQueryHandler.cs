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
        // Fetch all categories (soft-delete filter is applied by EF query filter)
        var allCategories = await categoryRepository.GetAllAsync(
            filter: request.ActiveOnly ? c => c.IsActive : null,
            orderBy: q => q.OrderBy(c => c.SortOrder).ThenBy(c => c.Name),
            ct: ct);

        // Group by parent for O(n) tree building
        var byParent = allCategories.ToLookup(c => c.ParentCategoryId);

        // Build tree from roots (ParentCategoryId == null)
        var roots = byParent[null]
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToList();

        var tree = roots
            .Select(r => BuildNode(r, byParent))
            .ToList() as IReadOnlyList<CategoryDto>;

        return Result<IReadOnlyList<CategoryDto>>.Success(tree);
    }

    private static CategoryDto BuildNode(
        Domain.Entities.Category category,
        ILookup<Guid?, Domain.Entities.Category> byParent)
    {
        var children = byParent[category.Id]
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => BuildNode(c, byParent))
            .ToList() as IReadOnlyList<CategoryDto>;

        return new CategoryDto(
            category.Id,
            category.Name,
            category.Slug,
            category.Icon,
            category.SortOrder,
            category.IsActive,
            category.ParentCategoryId,
            children);
    }
}
