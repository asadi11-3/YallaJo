namespace ContentCore.Application.Commands.Category.UpdateCategory;

public sealed record UpdateCategoryResult(
    Guid Id,
    string Name,
    string Slug);
