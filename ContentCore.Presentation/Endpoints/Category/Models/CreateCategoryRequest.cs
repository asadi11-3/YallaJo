namespace ContentCore.Presentation.Endpoints.Category.Models;

public sealed record CreateCategoryRequest(
    string Name,
    string? Slug = null,
    Guid? ParentCategoryId = null,
    string? Icon = null,
    int SortOrder = 0,
    string? SourceLanguageCode = null);
