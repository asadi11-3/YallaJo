namespace YallaJo.Web.Areas.Admin.Models.SeoSitemap;

public static class SeoSitemapMapper
{
    public static SeoSitemapVm ToVm(PaginatedSitemapResponse page, SitemapFilterRequest filter)
    {
        return new SeoSitemapVm
        {
            Page = page.PageNumber,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount,
            EntityTypeFilter = filter.EntityType,
            IsActiveFilter = filter.IsActive,
            Entries = page.Items.Select(e => new SitemapEntryRowVm
            {
                Id = e.Id,
                Url = e.Url,
                EntityType = e.EntityType,
                EntityId = e.EntityId,
                Priority = e.Priority,
                ChangeFrequency = e.ChangeFrequency,
                LastModified = e.LastModified,
                IsActive = e.IsActive
            }).ToList()
        };
    }
}
