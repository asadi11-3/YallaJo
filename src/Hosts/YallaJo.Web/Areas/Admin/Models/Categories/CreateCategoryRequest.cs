namespace YallaJo.Web.Areas.Admin.Models.Categories;

public sealed record CreateCategoryRequest(
    string Name,
    string? Slug,
    Guid? ParentCategoryId,
    string? Icon,
    int SortOrder,
    string? SourceLanguageCode);
