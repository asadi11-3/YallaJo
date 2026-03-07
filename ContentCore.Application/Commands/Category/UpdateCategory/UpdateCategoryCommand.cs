using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Commands.Category.UpdateCategory;

public sealed record UpdateCategoryResult(
    Guid Id,
    string Name,
    string Slug);

public sealed record UpdateCategoryCommand(
    Guid Id,
    string Name,
    string Slug,
    string? Icon = null,
    int? SortOrder = null,
    string SourceLanguageCode = "en") : ICommand<UpdateCategoryResult>;
