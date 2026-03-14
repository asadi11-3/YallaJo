using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Queries.Category.ListCategories;

public sealed record CategoryDto
{
    public CategoryDto(
        Guid id,
        string name,
        string slug,
        string? icon,
        int sortOrder,
        bool isActive,
        Guid? parentCategoryId)
        : this(id, name, slug, icon, sortOrder, isActive, parentCategoryId, Array.Empty<CategoryDto>())
    {
    }

    public CategoryDto(
        Guid id,
        string name,
        string slug,
        string? icon,
        int sortOrder,
        bool isActive,
        Guid? parentCategoryId,
        IReadOnlyList<CategoryDto> children)
    {
        Id = id;
        Name = name;
        Slug = slug;
        Icon = icon;
        SortOrder = sortOrder;
        IsActive = isActive;
        ParentCategoryId = parentCategoryId;
        Children = children;
    }

    public Guid Id { get; init; }
    public string Name { get; init; }
    public string Slug { get; init; }
    public string? Icon { get; init; }
    public int SortOrder { get; init; }
    public bool IsActive { get; init; }
    public Guid? ParentCategoryId { get; init; }
    public IReadOnlyList<CategoryDto> Children { get; init; }
}

public sealed record ListCategoriesQuery(bool ActiveOnly = false) : IQuery<IReadOnlyList<CategoryDto>>;
