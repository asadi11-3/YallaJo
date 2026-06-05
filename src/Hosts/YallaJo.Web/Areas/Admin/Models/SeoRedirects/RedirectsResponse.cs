namespace YallaJo.Web.Areas.Admin.Models.SeoRedirects;

public sealed class PaginatedRedirectsResponse
{
    public IReadOnlyList<RedirectItemResponse> Items { get; set; } = [];
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}

public sealed class RedirectItemResponse
{
    public Guid Id { get; set; }
    public string OldUrl { get; set; } = "";
    public string NewUrl { get; set; } = "";
    public int StatusCode { get; set; }
    public bool IsActive { get; set; }
    public long HitCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>Response body for POST /api/v1/seo/redirects.</summary>
public sealed class CreateRedirectResponse
{
    public Guid Id { get; set; }
}
