using ContentSeo.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;

namespace ContentSeo.Domain.Entities;

public sealed class FaqItem : AuditableEntity, IAggregateRoot
{
    private readonly List<FaqItemTranslation> _translations = [];

    private FaqItem() { } // EF Core

    public SeoEntityType EntityType { get; private set; }
    public Guid EntityId { get; private set; }
    public string Question { get; private set; } = string.Empty;
    public string Answer { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    public IReadOnlyCollection<FaqItemTranslation> FaqItemTranslations => _translations.AsReadOnly();
}
