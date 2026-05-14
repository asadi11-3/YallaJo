using ContentCore.Application.Caching;
using ContentCore.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Queries.Translation.GetEntityTranslations;

public sealed record GetEntityTranslationsQuery(
    string EntityType,
    Guid EntityId,
    string? LanguageCode = null,
    TranslationStatus? Status = null)
    : IQuery<IReadOnlyList<EntityTranslationDto>>, ICacheableQuery
{
    public string CacheKey => (LanguageCode is null && Status is null)
        ? ContentCoreCacheKeys.EntityTranslations(EntityType, EntityId)
        : ContentCoreCacheKeys.EntityTranslations(EntityType, EntityId, LanguageCode, Status);

    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(120);
    public IReadOnlyList<string> Tags =>
    [
        ContentCoreCacheKeys.TranslationsTag,
        ContentCoreCacheKeys.EntityTranslationsTag(EntityType, EntityId),
    ];
}
