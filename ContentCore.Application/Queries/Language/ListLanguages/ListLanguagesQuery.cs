using ContentCore.Application.Caching;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Queries.Language.ListLanguages;

public sealed record LanguageDto(
    Guid Id,
    string Code,
    string Name,
    string NativeName,
    bool IsRtl,
    bool IsActive);

public sealed record ListLanguagesQuery(bool ActiveOnly = true)
    : IQuery<IReadOnlyList<LanguageDto>>, ICacheableQuery
{
    public string CacheKey => ContentCoreCacheKeys.Languages(ActiveOnly);
    // Languages rarely change — cache for 1 hour
    public TimeSpan? CacheDuration => TimeSpan.FromHours(1);
    public IReadOnlyList<string> Tags => ["languages"];
}
