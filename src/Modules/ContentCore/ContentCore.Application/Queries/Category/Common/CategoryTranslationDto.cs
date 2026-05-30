namespace ContentCore.Application.Queries.Category.Common;

/// <summary>A single language translation of a category's name and slug.</summary>
public sealed record CategoryTranslationDto(
    Guid LanguageId,
    string Name,
    string Slug);
