using System.Security.Cryptography.X509Certificates;
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

    public static Category Create(
        Guid id,
        Guid? parentCategoryId,
        string name,
        string slug,
        string icon,
        int sortOreder) {

        if (string.IsNullOrEmpty(name)) 
        { throw new ArgumentException("Category name is required.", nameof(name)); }
        
        if (string.IsNullOrEmpty(slug)) 
        { throw new ArgumentException("Category slug is required.", nameof(slug)); }

        return new Category { 
            Id = id,
            ParentCategoryId = parentCategoryId,
            Name = name.Trim(),
            Slug = slug.Trim(),
            Icon = icon.Trim(),
            SortOrder = sortOreder,
            IsActive = true
        };
    }

    public void AddTranslation(Guid id, Guid languageId, string name, string slug)
    {
        if (_translations.Any(x => x.LanguageId == languageId))
            throw new InvalidOperationException("Translation already exists for this language.");
        _translations.Add(CategoryTranslation.Create(id, Id, languageId, name, slug));
    }
}
