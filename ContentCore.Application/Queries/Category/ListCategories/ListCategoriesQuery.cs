using ContentCore.Application.Caching;
using ContentCore.Application.Queries.Category.Common;
using CategoryEntity = ContentCore.Domain.Entities.Category;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Queries.Category.ListCategories;

/// <summary>
/// Category DTO returned in list and get-by-id responses.
/// Translations are populated only when the query requests them.
/// </summary>
public sealed class CategoryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Icon { get; init; }
    public int SortOrder { get; init; }
    public bool IsActive { get; init; }
    public Guid? ParentCategoryId { get; init; }

    /// <summary>All available translations. Empty when the query did not request translations.</summary>
    public IReadOnlyList<CategoryTranslationDto> Translations { get; init; } = [];

    /// <summary>Direct children in tree responses. Empty for leaf nodes.</summary>
    public IReadOnlyList<CategoryDto> Children { get; init; } = [];

    public  CategoryDto() { }

    /// <summary>
    /// Maps a Category entity to a DTO.
    /// Translations are populated when the entity's navigation property is loaded.
    /// </summary>
    public static CategoryDto From(CategoryEntity category, IReadOnlyList<CategoryDto> children)
        => new()
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            Icon = category.Icon,
            SortOrder = category.SortOrder,
            IsActive = category.IsActive,
            ParentCategoryId = category.ParentCategoryId,
            Translations = category.Translations
                .Select(t => new CategoryTranslationDto(t.LanguageId, t.Name, t.Slug))
                .ToList(),
            Children = children
        };
}

public sealed record ListCategoriesQuery(
    bool ActiveOnly = false,
    Guid? ParentCategoryId = null,
    bool WithTranslations = false)
    : IQuery<IReadOnlyList<CategoryDto>>, ICacheableQuery
{
    public string CacheKey => ContentCoreCacheKeys.CategoryList(ActiveOnly, ParentCategoryId, WithTranslations);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(30);
    public IReadOnlyList<string> Tags => ["categories"];
}
