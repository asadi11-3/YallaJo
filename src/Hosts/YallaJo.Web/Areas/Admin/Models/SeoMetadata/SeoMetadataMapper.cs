namespace YallaJo.Web.Areas.Admin.Models.SeoMetadata;

public static class SeoMetadataMapper
{
    public static SeoMetadataDetailVm ToDetail(SeoMetadataItemResponse r) => new()
    {
        Id = r.Id,
        EntityType = r.EntityType,
        EntityId = r.EntityId,
        MetaTitle = r.MetaTitle,
        MetaDescription = r.MetaDescription,
        OgTitle = r.OgTitle,
        OgDescription = r.OgDescription,
        OgImageUrl = r.OgImageUrl,
        SchemaMarkup = r.SchemaMarkup,
        SitemapPriority = r.SitemapPriority,
        SitemapChangeFrequency = r.SitemapChangeFrequency,
        CanonicalUrl = r.CanonicalUrl,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt,
    };

    public static SeoMetadataFormVm ToForm(SeoMetadataItemResponse r) => new()
    {
        EntityType = r.EntityType,
        EntityId = r.EntityId,
        MetaTitle = r.MetaTitle,
        MetaDescription = r.MetaDescription,
        OgTitle = r.OgTitle,
        OgDescription = r.OgDescription,
        OgImageUrl = r.OgImageUrl,
        SchemaMarkup = r.SchemaMarkup,
        SitemapPriority = r.SitemapPriority,
        SitemapChangeFrequency = r.SitemapChangeFrequency,
        CanonicalUrl = r.CanonicalUrl,
    };
}
