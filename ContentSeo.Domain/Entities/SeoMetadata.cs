using ContentSeo.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;

namespace ContentSeo.Domain.Entities;

public sealed class SeoMetadata : AuditableEntity , IAggregateRoot
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

    // ── Factory ───────────────────────────────────────────────────────────────

    public static SeoMetadata Create(
        SeoEntityType entityType,
        Guid entityId,
        string? metaTitle = null,
        string? metaDescription = null,
        string? canonicalUrl = null,
        decimal sitemapPriority = 0.5m,
        string? sitemapChangeFrequency = "weekly")
    {
        if (entityId == Guid.Empty)
            throw new ArgumentException("EntityId cannot be empty.", nameof(entityId));

        if (sitemapPriority is < 0m or > 1m)
            throw new ArgumentOutOfRangeException(
                nameof(sitemapPriority), "SitemapPriority must be between 0.0 and 1.0.");

        return new SeoMetadata
        {
            Id                     = Guid.CreateVersion7(),
            EntityType             = entityType,
            EntityId               = entityId,
            MetaTitle              = metaTitle?.Trim(),
            MetaDescription        = metaDescription?.Trim(),
            CanonicalUrl           = canonicalUrl?.Trim(),
            SitemapPriority        = sitemapPriority,
            SitemapChangeFrequency = sitemapChangeFrequency,
        };
    }

    // ── Business Methods ──────────────────────────────────────────────────────

    /// <summary>
    /// Updates meta fields. Only call when you WANT to clobber editor-managed content.
    /// Integration event handlers should NOT call this — editors own MetaTitle after creation.
    /// </summary>
    public void UpdateMeta(string? metaTitle, string? metaDescription)
    {
        MetaTitle       = metaTitle?.Trim();
        MetaDescription = metaDescription?.Trim();
        MarkUpdated();
    }
}
