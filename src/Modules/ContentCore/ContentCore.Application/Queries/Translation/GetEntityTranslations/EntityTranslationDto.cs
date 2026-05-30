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
