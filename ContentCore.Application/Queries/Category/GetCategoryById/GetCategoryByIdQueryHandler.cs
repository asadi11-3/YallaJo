using ContentCore.Application.Queries.Category.ListCategories;
using ContentCore.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Application.Queries.Category.GetCategoryById;

public sealed class GetCategoryByIdQueryHandler(ICategoryRepository categoryRepository)
    : IQueryHandler<GetCategoryByIdQuery, CategoryDto>
{
    public async Task<Result<CategoryDto>> Handle(
        GetCategoryByIdQuery request,
        CancellationToken ct)
    {
        var category = await categoryRepository.GetByIdAsync(request.Id, ct);
        if (category is null)
            return Result<CategoryDto>.NotFound($"Category '{request.Id}' not found.");

        var dto = new CategoryDto(
            category.Id, category.Name, category.Slug, category.Icon,
            category.SortOrder, category.IsActive, category.ParentCategoryId);

        return Result<CategoryDto>.Success(dto);
    }
}
