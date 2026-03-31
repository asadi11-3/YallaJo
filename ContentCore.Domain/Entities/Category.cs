using ContentCore.Domain.Events;
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

    // ── Factory Method (the ONLY way to create) ──
    public static Category Create(
        string name,
        string slug,
        string sourceLanguageCode,
        Guid? parentCategoryId = null,
        int sortOrder = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name is required.", nameof(name));

        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Category slug is required.", nameof(slug));

        var category = new Category
        {
            ParentCategoryId = parentCategoryId,
            Name = name.Trim(),
            Slug = slug.Trim(),
            SortOrder = sortOrder,
            IsActive = true
        };

        category.AddDomainEvent(new CategoryCreatedDomainEvent(
            category.Id, category.Name, sourceLanguageCode));

        return category;
    }

    // ── Business Methods ──
    public void Update(string name, string slug, string sourceLanguageCode)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name is required.", nameof(name));

        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Category slug is required.", nameof(slug));

        Name = name.Trim();
        Slug = slug.Trim();
        MarkUpdated();

        AddDomainEvent(new CategoryUpdatedDomainEvent(Id, Name, sourceLanguageCode));
    }

    public void SetIcon(string? icon)
    {
        Icon = icon?.Trim();
        MarkUpdated();
    }

    public void SetSortOrder(int sortOrder)
    {
        SortOrder = sortOrder;
        MarkUpdated();
    }

    public void Activate()
    {
        IsActive = true;
        MarkUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkUpdated();
    }

    public void AddTranslation(Guid languageId, string name, string slug)
    {
        if (_translations.Any(x => x.LanguageId == languageId))
            throw new InvalidOperationException("Translation already exists for this language.");
        _translations.Add(CategoryTranslation.Create(Id, languageId, name, slug));
    }

    public void UpdateTranslation(Guid languageId, string name, string slug)
    {
        var translation = _translations.FirstOrDefault(x => x.LanguageId == languageId);
        if (translation == null)
            throw new InvalidOperationException($"Translation for language '{languageId}' not found.");
        translation.Update(name, slug);
    }

    public void ChangeParent(Guid? parentCategoryId)
    {
        ParentCategoryId = parentCategoryId;
        MarkUpdated();
    }

  
    public static string GenerateSlug(string name) =>
        System.Text.RegularExpressions.Regex
            .Replace(name.Trim().ToLowerInvariant().Replace(' ', '-'), @"[^a-z0-9\-]", string.Empty)
            .Trim('-');
}
