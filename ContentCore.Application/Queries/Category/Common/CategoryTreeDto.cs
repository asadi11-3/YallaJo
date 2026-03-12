namespace ContentCore.Application.Queries.Category.Common;

public sealed record CategoryTreeDto(
    Guid Id,
    Guid? ParentCategoryId,
    string Name,
    string Slug,
    string? Icon,
    int SortOrder,
    bool IsActive,
    IReadOnlyList<CategoryTranslationDto> Translations,
    List<CategoryTreeDto> Children
);