namespace ContentSeo.Presentation.Endpoints.Redirect.Models;

public sealed record UpdateRedirectRequest(
    string? NewUrl,
    int? StatusCode,
    bool? IsActive);
