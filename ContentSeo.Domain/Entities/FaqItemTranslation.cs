using YallaJo.SharedKernel.Domain.Entities;

namespace ContentSeo.Domain.Entities;

public sealed class FaqItemTranslation : BaseEntity
{
    private FaqItemTranslation() { } // EF Core

    public Guid FaqItemId { get; private set; }
    public Guid LanguageId { get; private set; }
    public string Question { get; private set; } = string.Empty;
    public string Answer { get; private set; } = string.Empty;

    public FaqItem FaqItem { get; private set; } = default!;
}
