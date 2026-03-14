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
        // Load category — include translations when client requested them
        var category = request.WithTranslations
            ? await categoryRepository.GetByIdWithTranslationsAsync(request.Id, ct)
            : await categoryRepository.GetByIdAsync(request.Id, ct);

        if (category is null)
            return Result<CategoryDto>.NotFound($"Category '{request.Id}' not found.");

        // Load direct subcategories (separate query — avoids loading the entire tree)
        var subcategories = request.WithTranslations
            ? await categoryRepository.GetAllWithTranslationsAsync(
                filter: c => c.ParentCategoryId == request.Id,
                orderBy: q => q.OrderBy(c => c.SortOrder).ThenBy(c => c.Name),
                ct: ct)
            : await categoryRepository.GetAllAsync(
                filter: c => c.ParentCategoryId == request.Id,
                orderBy: q => q.OrderBy(c => c.SortOrder).ThenBy(c => c.Name),
                ct: ct);

        var childDtos = subcategories
            .Select(c => CategoryDto.From(c, []))
            .ToList() as IReadOnlyList<CategoryDto>;

        return Result<CategoryDto>.Success(CategoryDto.From(category, childDtos));
    }
}
