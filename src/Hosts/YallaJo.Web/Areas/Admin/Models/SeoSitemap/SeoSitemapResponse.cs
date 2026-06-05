namespace YallaJo.Web.Areas.Admin.Models.SeoSitemap;

public sealed class PaginatedSitemapResponse
{
    public IReadOnlyList<SitemapEntryItemResponse> Items { get; set; } = [];
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}

public sealed class SitemapEntryItemResponse
{
    public Guid Id { get; set; }
    public string Url { get; set; } = "";
    public string EntityType { get; set; } = "";
    public Guid? EntityId { get; set; }
    public decimal? Priority { get; set; }
    public string? ChangeFrequency { get; set; }
    public DateTime? LastModified { get; set; }
    public bool IsActive { get; set; }
}

public sealed class RegenerateSitemapResponse
{
    public int UrlCount { get; set; }
    public DateTime RenderedAt { get; set; }
}
