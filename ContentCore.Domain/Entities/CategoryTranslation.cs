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
}
