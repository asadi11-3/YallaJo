using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Queries.Category.ListCategories;

public sealed record CategoryDto(
    Guid Id,
    string Name,
    string Slug,
    string? Icon,
    int SortOrder,
    bool IsActive,
    Guid? ParentCategoryId);

public sealed record ListCategoriesQuery(bool ActiveOnly = false) : IQuery<IReadOnlyList<CategoryDto>>;
