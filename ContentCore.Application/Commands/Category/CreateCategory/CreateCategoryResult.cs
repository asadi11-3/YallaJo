namespace ContentCore.Application.Commands.Category.CreateCategory;

public sealed record CreateCategoryResult(
    Guid Id,
    string Name,
    string Slug);
