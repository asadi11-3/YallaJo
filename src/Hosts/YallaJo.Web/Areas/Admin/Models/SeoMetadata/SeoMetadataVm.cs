using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Admin.Models.SeoMetadata;

public sealed class SeoMetadataVm
{
    public SeoEntityType EntityType { get; set; } = SeoEntityType.Place;
    public Guid? EntityId { get; set; }
    public bool HasQueried { get; set; }
    public SeoMetadataDetailVm? Metadata { get; set; }
    public SeoMetadataFormVm Form { get; set; } = new();
    public bool Exists => Metadata is not null;
}

public sealed class SeoMetadataDetailVm
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

public sealed class SeoMetadataFormVm
{
    [Required]
    [Display(Name = "Entity type")]
    public SeoEntityType EntityType { get; set; } = SeoEntityType.Place;

    [Required]
    [Display(Name = "Entity id")]
    public Guid EntityId { get; set; }

    [StringLength(200)]
    [Display(Name = "Meta title")]
    public string? MetaTitle { get; set; }

    [StringLength(500)]
    [Display(Name = "Meta description")]
    public string? MetaDescription { get; set; }

    [StringLength(200)]
    [Display(Name = "Open Graph title")]
    public string? OgTitle { get; set; }

    [StringLength(500)]
    [Display(Name = "Open Graph description")]
    public string? OgDescription { get; set; }

    [StringLength(2048)]
    [Display(Name = "Open Graph image URL")]
    public string? OgImageUrl { get; set; }

    [Display(Name = "Schema markup (JSON-LD)")]
    public string? SchemaMarkup { get; set; }

    [Range(0.0, 1.0)]
    [Display(Name = "Sitemap priority")]
    public decimal SitemapPriority { get; set; } = 0.5m;

    [StringLength(20)]
    [Display(Name = "Sitemap change frequency")]
    public string? SitemapChangeFrequency { get; set; }

    [StringLength(2048)]
    [Display(Name = "Canonical URL")]
    public string? CanonicalUrl { get; set; }
}
