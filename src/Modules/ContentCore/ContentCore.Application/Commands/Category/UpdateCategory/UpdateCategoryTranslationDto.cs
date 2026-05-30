namespace ContentCore.Application.Commands.Category.UpdateCategory;

public sealed record UpdateCategoryTranslationDto(
    Guid LanguageId,
    string Name,
    string Slug);
