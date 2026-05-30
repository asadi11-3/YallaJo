using ContentCore.Application.Caching;
using ContentCore.Application.Queries.Category.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Queries.Category.GetCategoryById;

public sealed record GetCategoryByIdQuery(
    Guid Id,
    bool WithTranslations = false,
    bool IncludeInactive = false)
    : IQuery<CategoryDto>, ICacheableQuery
{
    // Cache key varies by IncludeInactive so admin/public views don't share the same entry.
    public string CacheKey => ContentCoreCacheKeys.Category(Id, WithTranslations, IncludeInactive);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags =>
    [
        ContentCoreCacheKeys.CategoriesTag,
        ContentCoreCacheKeys.CategoryTag(Id),
    ];
}
