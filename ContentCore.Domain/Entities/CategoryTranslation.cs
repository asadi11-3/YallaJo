using YallaJo.SharedKernel.Domain.Entities;

namespace ContentCore.Domain.Entities;

public sealed class CategoryTranslation : BaseEntity
{
    private CategoryTranslation() { } // EF Core

    public Guid CategoryId { get; private set; }
    public Guid LanguageId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;

    public Category Category { get; private set; } = default!;


    public static CategoryTranslation Create(
      Guid id,
      Guid categoryId,
      Guid languageId,
      string name,
      string slug)
    {
        if (languageId == Guid.Empty)
            throw new ArgumentException("Language is required.", nameof(languageId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Translation name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Translation slug is required.", nameof(slug));
        return new CategoryTranslation
        {
            Id = id,
            CategoryId = categoryId,
            LanguageId = languageId,
            Name = name.Trim(),
            Slug = slug.Trim()
        };
    }
}
