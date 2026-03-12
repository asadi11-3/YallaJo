using ContentCore.Application.Queries.Category.Common;
using ContentCore.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Application.Queries.Category.GetCategories;

public sealed class GetCategoriesQueryHandler(
    ICategoryRepository categoryRepository)
    : IQueryHandler<GetCategoriesQuery, GetCategoriesResponse>
{
    public async Task<Result<GetCategoriesResponse>> Handle(
        GetCategoriesQuery request,
        CancellationToken ct)
    {
        // ملاحظة: نجيب كل الكاتيجوري مع الترجمات من الريبو
        var categories = await categoryRepository.GetAllWithTranslationsAsync(ct);

        // ملاحظة: فلترة حسب active إذا انبعتت من الطلب
        if (request.IsActive.HasValue)
        {
            categories = categories
                .Where(c => c.IsActive == request.IsActive.Value)
                .ToList();
        }

        // ملاحظة: نحول كل الكاتيجوري إلى DTO أولًا
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

        // ملاحظة: هنا نربط كل child مع parent تبعه
        foreach (var category in categories)
        {
            if (category.ParentCategoryId.HasValue &&
                lookup.TryGetValue(category.ParentCategoryId.Value, out var parentDto))
            {
                parentDto.Children.Add(lookup[category.Id]);
            }
        }

        List<CategoryTreeDto> result;

        if (request.ParentCategoryId.HasValue)
        {
            // ملاحظة: إذا انبعث parentId نرجّع أبناء هذا الأب فقط
            result = lookup.Values
                .Where(c => c.ParentCategoryId == request.ParentCategoryId.Value)
                .OrderBy(c => c.SortOrder)
                .ToList();
        }
        else
        {
            // ملاحظة: بدون parentId نرجّع الجذور فقط ومعهم children nested
            result = lookup.Values
                .Where(c => c.ParentCategoryId == null)
                .OrderBy(c => c.SortOrder)
                .ToList();
        }

        return Result<GetCategoriesResponse>.Success(
            new GetCategoriesResponse(result));
    }
}