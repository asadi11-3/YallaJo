using ContentCore.Application.Caching;
using ContentCore.Application.Queries.Category.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Queries.Category.GetCategoryById;

public sealed record GetCategoryByIdQuery(Guid Id, bool WithTranslations = false)
    : IQuery<CategoryDto>, ICacheableQuery
{
    public string CacheKey => ContentCoreCacheKeys.Category(Id, WithTranslations);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags => ["categories", $"category:{Id}"];
}
