using YallaJo.SharedKernel.Domain.Entities;

namespace ContentSeo.Domain.Entities;

public sealed class SitemapEntry : AuditableEntity
{
    private SitemapEntry() { } // EF Core

    public string Url { get; private set; } = string.Empty;
    public string? ChangeFrequency { get; private set; }
    public decimal? Priority { get; private set; }
    public DateTime? LastModified { get; private set; }
    public string EntityType { get; private set; } = string.Empty;
    public Guid? EntityId { get; private set; }
    public bool IsActive { get; private set; } = true;
}
