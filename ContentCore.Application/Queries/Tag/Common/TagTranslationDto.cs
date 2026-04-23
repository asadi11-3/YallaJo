namespace ContentCore.Application.Queries.Tag.Common;

/// <summary>A single language translation of a tag's name and slug.</summary>
public sealed record TagTranslationDto(
    Guid LanguageId,
    string Name,
    string Slug);
