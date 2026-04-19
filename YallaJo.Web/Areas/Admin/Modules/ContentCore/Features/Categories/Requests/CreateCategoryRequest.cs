namespace YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Categories.Requests;

public sealed record CreateCategoryRequest(
    string Name,
    string? Slug,
    Guid? ParentCategoryId,
    string? Icon,
    int SortOrder,
    string? SourceLanguageCode);
