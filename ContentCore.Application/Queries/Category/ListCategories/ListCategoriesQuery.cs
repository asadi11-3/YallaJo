using ContentCore.Application.Caching;
using ContentCore.Application.Queries.Category.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Queries.Category.ListCategories;

public sealed record ListCategoriesQuery(
    bool ActiveOnly = true,      // Default is true — callers must explicitly opt-in to see inactive
    Guid? ParentCategoryId = null,
    bool WithTranslations = false)
    : IQuery<IReadOnlyList<CategoryDto>>, ICacheableQuery
{
    public string CacheKey => ContentCoreCacheKeys.CategoryList(ActiveOnly, ParentCategoryId, WithTranslations);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(30);
    public IReadOnlyList<string> Tags => ["categories"];
}
