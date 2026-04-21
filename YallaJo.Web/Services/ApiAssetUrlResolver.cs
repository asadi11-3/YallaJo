using Microsoft.Extensions.Configuration;

namespace YallaJo.Web.Services;

/// <summary>
/// Resolves relative asset URLs returned by the API (e.g. <c>/uploads/avatars/…</c>)
/// into absolute URLs the browser can fetch.
/// <para>
/// The API stores files under its own wwwroot and exposes them via
/// <c>/uploads/{folder}/{file}</c>. When the Web host renders <c>&lt;img src&gt;</c>
/// with a relative path, the browser requests it from the WEB origin — where the
/// file does not exist — and the image breaks. This resolver prepends the
/// configured <c>ApiBaseUrl</c> so the browser hits the API origin instead.
/// </para>
/// </summary>
public interface IApiAssetUrlResolver
{
    /// <summary>
    /// Returns an absolute URL when the input is a relative API-hosted path.
    /// Absolute URLs (http/https) and null/whitespace pass through unchanged.
    /// </summary>
    string? Resolve(string? url);
}

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

        // No API base configured — preserve legacy behavior.
        if (string.IsNullOrEmpty(_apiBaseUrl)) return url;

        var relative = url.StartsWith('/') ? url : "/" + url;
        return _apiBaseUrl + relative;
    }
}
