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

public sealed record GetEntityTranslationsQuery(
    string EntityType,
    Guid EntityId) : IQuery<IReadOnlyList<EntityTranslationDto>>;
