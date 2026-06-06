namespace YallaJo.Web.Infrastructure.Seo;

/// <summary>
/// Page-level SEO metadata. Populate this in a controller/facade or view and push it
/// into <see cref="Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary"/> via
/// <see cref="SeoViewData.SetSeo"/>; the shared layout head renders the tags.
/// </summary>
public sealed record SeoModel
{
    /// <summary>Page title (rendered as &lt;title&gt; and og:title). Truncated to 60 chars.</summary>
    public required string Title { get; init; }

    /// <summary>Meta description / og:description. Truncated to 160 chars.</summary>
    public string? Description { get; init; }

    /// <summary>Absolute canonical URL (also emitted as og:url).</summary>
    public string? Canonical { get; init; }

    /// <summary>Absolute og:image URL (1200x630+ recommended).</summary>
    public string? OgImage { get; init; }

    /// <summary>OpenGraph type (website, article, product, ...).</summary>
    public string OgType { get; init; } = "website";

    /// <summary>Optional robots directive (e.g. "noindex, nofollow").</summary>
    public string? Robots { get; init; }

    /// <summary>Optional JSON-LD payload (already serialized JSON). Emitted verbatim.</summary>
    public string? JsonLd { get; init; }
}
