using ContentSeo.Domain.Enums;
using ContentSeo.Domain.Events;
using YallaJo.SharedKernel.Domain.Entities;

namespace ContentSeo.Domain.Entities;

public sealed class SeoMetadata : AuditableEntity, IAggregateRoot
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

        var meta = new SeoMetadata
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

        meta.AddDomainEvent(new SeoMetadataCreatedDomainEvent(meta.Id, meta.EntityType, meta.EntityId));

        return meta;
    }

    // ── Business Methods ──────────────────────────────────────────────────────

    /// <summary>
    /// Updates meta fields. Only call when you WANT to clobber editor-managed content.
    /// Integration event handlers should NOT call this — editors own MetaTitle after creation.
    /// </summary>
    public void UpdateMeta(string? metaTitle, string? metaDescription)
    {
        EnsureNotDeleted();
        MetaTitle       = metaTitle?.Trim();
        MetaDescription = metaDescription?.Trim();
        MarkUpdated();
        AddDomainEvent(new SeoMetadataUpdatedDomainEvent(Id, EntityType, EntityId));
    }

    public void UpdateOg(string? ogTitle, string? ogDescription, string? ogImageUrl)
    {
        EnsureNotDeleted();
        OgTitle       = ogTitle?.Trim();
        OgDescription = ogDescription?.Trim();
        OgImageUrl    = ogImageUrl?.Trim();
        MarkUpdated();
        AddDomainEvent(new SeoMetadataUpdatedDomainEvent(Id, EntityType, EntityId));
    }

    public void UpdateSchema(string? schemaMarkup)
    {
        EnsureNotDeleted();
        SchemaMarkup = schemaMarkup?.Trim();
        MarkUpdated();
        AddDomainEvent(new SeoMetadataUpdatedDomainEvent(Id, EntityType, EntityId));
    }

    public void UpdateSitemapHints(
        decimal sitemapPriority,
        string? sitemapChangeFrequency,
        string? canonicalUrl)
    {
        EnsureNotDeleted();

        if (sitemapPriority is < 0m or > 1m)
            throw new ArgumentOutOfRangeException(
                nameof(sitemapPriority), "SitemapPriority must be between 0.0 and 1.0.");

        SitemapPriority        = sitemapPriority;
        SitemapChangeFrequency = sitemapChangeFrequency;
        CanonicalUrl           = canonicalUrl?.Trim();
        MarkUpdated();
        AddDomainEvent(new SeoMetadataUpdatedDomainEvent(Id, EntityType, EntityId));
    }

    /// <summary>
    /// Soft-deletes this SEO metadata record and emits <see cref="SeoMetadataDeletedDomainEvent"/>.
    /// Throws if the record is already deleted; callers should pre-check <see cref="AuditableEntity{TKey}.IsDeleted"/>
    /// (or treat NotFound==already-deleted at the application boundary).
    /// </summary>
    public void Delete()
    {
        EnsureNotDeleted();
        SoftDelete();
        AddDomainEvent(new SeoMetadataDeletedDomainEvent(Id, EntityType, EntityId));
    }

    // ── Guards ────────────────────────────────────────────────────────────────

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
            throw new InvalidOperationException(
                "SeoMetadata.Deleted: operation not permitted on a soft-deleted record.");
    }
}
