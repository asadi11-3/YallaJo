using ContentCore.Application.Commands.Category.UpdateCategory;

namespace ContentCore.Presentation.Endpoints.Category.Models;

public sealed record UpdateCategoryRequest(
    string Name,
    string Slug,
    Guid? ParentCategoryId = null,
    string? Icon = null,
    int? SortOrder = null,
    string? SourceLanguageCode = null,
    List<UpdateCategoryTranslationDto>? Translations = null);
