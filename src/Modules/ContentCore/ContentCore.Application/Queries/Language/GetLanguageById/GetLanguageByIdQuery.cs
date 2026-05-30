using ContentCore.Application.Caching;
using ContentCore.Application.Queries.Language.ListLanguages;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Queries.Language.GetLanguageById;

public sealed record GetLanguageByIdQuery(Guid Id)
    : IQuery<LanguageDto>, ICacheableQuery
{
    public string CacheKey => ContentCoreCacheKeys.LanguageById(Id);
    public TimeSpan? CacheDuration => TimeSpan.FromHours(1);
    public IReadOnlyList<string> Tags =>
    [
        ContentCoreCacheKeys.LanguagesTag,
        ContentCoreCacheKeys.LanguageTag(Id),
    ];
}
