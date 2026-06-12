using Microsoft.Extensions.Options;

namespace YallaJo.Web.Infrastructure.Maps;

/// <summary>
/// Injected into views (see the shared <c>_Map</c> partial) so they can read the
/// public Mapbox token without touching raw configuration — same pattern as
/// <c>IRecaptchaScriptService</c>.
/// </summary>
public interface IMapboxScriptService
{
    /// <summary>True when maps should render (a public token is configured).</summary>
    bool IsEnabled { get; }

    /// <summary>The public (pk.*) access token for Mapbox GL JS.</summary>
    string AccessToken { get; }
}

/// <inheritdoc cref="IMapboxScriptService" />
public sealed class MapboxScriptService : IMapboxScriptService
{
    public MapboxScriptService(IOptions<MapboxOptions> options)
    {
        var opts = options.Value;
        AccessToken = opts.AccessToken;
        IsEnabled = opts.IsEnabled;
    }

    public bool IsEnabled { get; }

    public string AccessToken { get; }
}
