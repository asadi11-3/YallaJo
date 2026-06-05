namespace YallaJo.Web.Areas.Admin.Models.SeoRedirects;

public sealed class RedirectsFilterRequest
{
    public string? OldUrl { get; set; }
    public bool? IsActive { get; set; }
    public int? StatusCode { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

/// <summary>Outbound request body for POST /api/v1/seo/redirects.</summary>
public sealed record CreateRedirectApiRequest(string OldUrl, string NewUrl, int StatusCode);

/// <summary>Outbound request body for PUT /api/v1/seo/redirects/{id}.</summary>
public sealed record UpdateRedirectApiRequest(string? NewUrl, int? StatusCode, bool? IsActive);
