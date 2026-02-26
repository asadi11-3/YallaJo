using ContentSeo.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;

namespace ContentSeo.Domain.Entities;

public sealed class SeoMetadata : AuditableEntity
{
    private SeoMetadata() { } // EF Core

    public SeoEntityType EntityType { get; private set; }
    public Guid EntityId { get; private set; }
    public string? MetaTitle { get; private set; }
    public string? MetaDescription { get; private set; }
    public string? CanonicalUrl { get; private set; }
    public string? OgTitle { get; private set; }
    public string? OgDescription { get; private set; }
    public string? OgImageUrl { get; private set; }
    public string? SchemaMarkup { get; private set; }
    public decimal SitemapPriority { get; private set; } = 0.5m;
    public string? SitemapChangeFrequency { get; private set; }
}
