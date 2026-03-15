using ContentCore.Application.Queries.Category.Common;
using ContentCore.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Application.Queries.Category.GetCategoryById;

public sealed class GetCategoryByIdQueryHandler(
    ICategoryRepository categoryRepository)
    : IQueryHandler<GetCategoryByIdQuery, GetCategoryByIdResponse>
{
    public async Task<Result<GetCategoryByIdResponse>> Handle(
        GetCategoryByIdQuery request,
        CancellationToken ct)
    {
        // نجيب كل الكاتيجوري مع الترجمات
        var categories = await categoryRepository.GetAllWithTranslationsAsync(ct);

        var target = categories.FirstOrDefault(c => c.Id == request.Id);

        if (target is null)
        {
            return Result<GetCategoryByIdResponse>.NotFound(
                $"Category '{request.Id}' not found.");
        }

        var lookup = categories.ToDictionary(
            c => c.Id,
            c => new CategoryTreeDto(
                c.Id,
                c.ParentCategoryId,
                c.Name,
                c.Slug,
                c.Icon,
                c.SortOrder,
                c.IsActive,
                c.Translations
                    .Select(t => new CategoryTranslationDto(
                        t.LanguageId,
                        t.Name,
                        t.Slug))
                    .ToList(),
                new List<CategoryTreeDto>()));

        foreach (var category in categories)
        {
            if (category.ParentCategoryId.HasValue &&
                lookup.TryGetValue(category.ParentCategoryId.Value, out var parentDto))
            {
                parentDto.Children.Add(lookup[category.Id]);
            }
        }

        return Result<GetCategoryByIdResponse>.Success(
            new GetCategoryByIdResponse(lookup[target.Id]));
    }
}