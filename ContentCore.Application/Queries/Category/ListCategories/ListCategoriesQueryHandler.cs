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
        var categories = await categoryRepository.GetAllAsync(
            filter: request.ActiveOnly ? c => c.IsActive : null,
            orderBy: q => q.OrderBy(c => c.SortOrder).ThenBy(c => c.Name),
            ct: ct);

        var dtos = categories
            .Select(c => new CategoryDto(
                c.Id, c.Name, c.Slug, c.Icon, c.SortOrder, c.IsActive, c.ParentCategoryId))
            .ToList() as IReadOnlyList<CategoryDto>;

        return Result<IReadOnlyList<CategoryDto>>.Success(dtos);
    }
}
