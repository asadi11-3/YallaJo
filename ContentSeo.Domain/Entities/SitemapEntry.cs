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

    // ── Factory ───────────────────────────────────────────────────────────────

    public static SitemapEntry Create(
        string url,
        string entityType,
        Guid? entityId = null,
        string? changeFrequency = "weekly",
        decimal? priority = 0.5m,
        bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("Url is required.", nameof(url));
        if (string.IsNullOrWhiteSpace(entityType))
            throw new ArgumentException("EntityType is required.", nameof(entityType));

        return new SitemapEntry
        {
            Id              = Guid.CreateVersion7(),
            Url             = url.Trim(),
            EntityType      = entityType.Trim(),
            EntityId        = entityId,
            ChangeFrequency = changeFrequency,
            Priority        = priority,
            LastModified    = DateTime.UtcNow,
            IsActive        = isActive,
        };
    }

    // ── Business Methods ──────────────────────────────────────────────────────

    /// <summary>Refreshes LastModified — tells search engines to re-crawl.</summary>
    public void Touch()
    {
        LastModified = DateTime.UtcNow;
        MarkUpdated();
    }

    /// <summary>Changes the public URL and refreshes LastModified. Used on slug rename.</summary>
    public void ChangeUrl(string newUrl)
    {
        if (string.IsNullOrWhiteSpace(newUrl))
            throw new ArgumentException("Url is required.", nameof(newUrl));

        Url          = newUrl.Trim();
        LastModified = DateTime.UtcNow;
        MarkUpdated();
    }

    /// <summary>Removes from sitemap.xml output (soft-delete equivalent for SEO).</summary>
    public void Deactivate()
    {
        IsActive     = false;
        LastModified = DateTime.UtcNow;
        MarkUpdated();
    }

    /// <summary>Re-adds to sitemap.xml output.</summary>
    public void Reactivate()
    {
        IsActive     = true;
        LastModified = DateTime.UtcNow;
        MarkUpdated();
    }
}
