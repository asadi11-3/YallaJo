using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Commands.Category.UpdateCategory;

public sealed record UpdateCategoryCommand(
    Guid Id,
    string Name,
    string Slug,
    Guid? ParentCategoryId = null,
    string? Icon = null,
    int? SortOrder = null,
    string SourceLanguageCode = "en",
    IReadOnlyList<UpdateCategoryTranslationDto>? Translations = null) : ICommand<UpdateCategoryResult>;
