using ContentCore.Application.Caching;
using ContentCore.Application.Queries.Category.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Queries.Category.ListCategories;

public sealed record ListCategoriesQuery(
    bool ActiveOnly = false,
    Guid? ParentCategoryId = null,
    bool WithTranslations = false)
    : IQuery<IReadOnlyList<CategoryDto>>, ICacheableQuery
{
    public string CacheKey => ContentCoreCacheKeys.CategoryList(ActiveOnly, ParentCategoryId, WithTranslations);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(30);
    public IReadOnlyList<string> Tags => ["categories"];
}
