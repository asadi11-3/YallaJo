namespace YallaJo.Web.Areas.Admin.Models.SeoMetadata;

public sealed class SeoMetadataLookupRequest
{
    public SeoEntityType EntityType { get; set; } = SeoEntityType.Place;
    public Guid? EntityId { get; set; }
}

public sealed record UpsertSeoMetadataApiRequest(
    SeoEntityType EntityType,
    Guid EntityId,
    string? MetaTitle,
    string? MetaDescription,
    string? OgTitle,
    string? OgDescription,
    string? OgImageUrl,
    string? SchemaMarkup,
    decimal SitemapPriority,
    string? SitemapChangeFrequency,
    string? CanonicalUrl);

public sealed record UpdateSeoMetadataApiRequest(
    string? MetaTitle,
    string? MetaDescription,
    string? OgTitle,
    string? OgDescription,
    string? OgImageUrl,
    string? SchemaMarkup,
    decimal SitemapPriority,
    string? SitemapChangeFrequency,
    string? CanonicalUrl);
