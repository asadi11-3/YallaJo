using Microsoft.Extensions.Configuration;

namespace YallaJo.Web.Services;

public sealed class ApiAssetUrlResolver : IApiAssetUrlResolver
{
    private readonly string _apiBaseUrl;

    public ApiAssetUrlResolver(IConfiguration configuration)
    {
        _apiBaseUrl = (configuration["ApiBaseUrl"] ?? string.Empty).TrimEnd('/');
    }

    public string? Resolve(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return url;

        // Already absolute — leave as-is.
        if (Uri.TryCreate(url, UriKind.Absolute, out var parsed) &&
            (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps))
        {
            return url;
        }

        // Web-host static assets (theme images, seeded placeholder galleries such as
        // "/assets/images/gallery/01.jpg") live in THIS app's wwwroot — never prefix
        // them with the API origin or they 404 on the API host (prod bug).
        if (url.StartsWith("/assets/", StringComparison.OrdinalIgnoreCase))
        {
            return url;
        }

        // No API base configured — preserve legacy behavior.
        if (string.IsNullOrEmpty(_apiBaseUrl)) return url;

        var relative = url.StartsWith('/') ? url : "/" + url;
        return _apiBaseUrl + relative;
    }
}
