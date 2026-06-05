namespace YallaJo.Web.Areas.Admin.Models.SeoRedirects;

public static class RedirectsMapper
{
    public static RedirectsVm ToVm(PaginatedRedirectsResponse page, RedirectsFilterRequest filter)
    {
        return new RedirectsVm
        {
            Page = page.PageNumber,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount,
            OldUrlFilter = filter.OldUrl,
            IsActiveFilter = filter.IsActive,
            Redirects = page.Items.Select(r => new RedirectRowVm
            {
                Id = r.Id,
                OldUrl = r.OldUrl,
                NewUrl = r.NewUrl,
                StatusCode = r.StatusCode,
                IsActive = r.IsActive,
                HitCount = r.HitCount,
                CreatedAt = r.CreatedAt,
            }).ToList(),
        };
    }

    public static string StatusBadge(int statusCode) => statusCode switch
    {
        301 or 308 => "success",
        302 or 307 => "info",
        _ => "secondary",
    };
}
