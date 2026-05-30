using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Commands.Category.CreateCategory;

public sealed record CreateCategoryCommand(
    string Name,
    string? Slug = null,
    Guid? ParentCategoryId = null,
    string? Icon = null,
    int SortOrder = 0,
    string SourceLanguageCode = "en") : ICommand<CreateCategoryResult>;
