namespace ContentCore.Application.Queries.Tag.Common;

public sealed record TagDto(
    Guid Id,
    string Name,
    string Slug,
    bool IsActive,
    IReadOnlyList<TagTranslationDto>? Translations = null);
