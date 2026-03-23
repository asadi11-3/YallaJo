using ContentCore.Application.Caching;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Queries.Translation.GetEntityTranslations;

public sealed record EntityTranslationDto(
    Guid Id,
    string OriginalText,
    string TranslatedText,
    string FromLanguage,
    string ToLanguage,
    string? FieldName,
    string Status,
    double? Confidence,
    DateTime CreatedAt);

public sealed record GetEntityTranslationsQuery(string EntityType, Guid EntityId)
    : IQuery<IReadOnlyList<EntityTranslationDto>>, ICacheableQuery
{
    public string CacheKey => ContentCoreCacheKeys.EntityTranslations(EntityType, EntityId);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(15);
    public IReadOnlyList<string> Tags => ["translations", $"translations:{EntityType}:{EntityId}"];
}
