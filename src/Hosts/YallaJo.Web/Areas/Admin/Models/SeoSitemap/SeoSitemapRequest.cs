namespace YallaJo.Web.Areas.Admin.Models.SeoSitemap;

public sealed class SitemapFilterRequest
{
    public string? EntityType { get; set; }
    public bool? IsActive { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public sealed record UpdateSitemapEntryApiRequest(decimal? Priority, string? ChangeFrequency);
