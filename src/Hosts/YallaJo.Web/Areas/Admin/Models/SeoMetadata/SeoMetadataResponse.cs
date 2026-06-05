namespace YallaJo.Web.Areas.Admin.Models.SeoMetadata;

public enum SeoEntityType : byte
{
    Place = 0,
    Tour = 1,
    Business = 2,
    Blog = 3,
    TourGuide = 4,
    Creator = 5,
}

public sealed class SeoMetadataItemResponse
{
    public Guid Id { get; set; }
    public SeoEntityType EntityType { get; set; }
    public Guid EntityId { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? OgTitle { get; set; }
    public string? OgDescription { get; set; }
    public string? OgImageUrl { get; set; }
    public string? SchemaMarkup { get; set; }
    public decimal SitemapPriority { get; set; }
    public string? SitemapChangeFrequency { get; set; }
    public string? CanonicalUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public sealed class UpsertSeoMetadataResponse
{
    public Guid Id { get; set; }
    public bool Created { get; set; }
}
