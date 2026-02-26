using YallaJo.SharedKernel.Domain.Entities;

namespace ContentCore.Domain.Entities;

public sealed class Category : AuditableEntity, IAggregateRoot
{
    private readonly List<Category> _subCategories = [];
    private readonly List<CategoryTranslation> _translations = [];

    private Category() { } // EF Core

    public Guid? ParentCategoryId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Icon { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    public Category? ParentCategory { get; private set; }
    public IReadOnlyCollection<Category> SubCategories => _subCategories.AsReadOnly();
    public IReadOnlyCollection<CategoryTranslation> Translations => _translations.AsReadOnly();
}
