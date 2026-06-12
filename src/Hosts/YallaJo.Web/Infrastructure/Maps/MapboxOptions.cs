namespace YallaJo.Web.Infrastructure.Maps;

/// <summary>
/// Web-side Mapbox GL JS options (V13 maps). Only the <b>public</b> access token
/// (pk.*) belongs here — Mapbox public tokens are designed to be shipped to the
/// browser. When the token is empty, every map container silently renders
/// nothing and pages fall back to their text-only location blocks
/// (progressive enhancement, mirrors <c>RecaptchaOptions</c>).
/// </summary>
public sealed class MapboxOptions
{
    public const string SectionName = "Mapbox";

    /// <summary>Public browser token (pk.*). Empty string disables all maps.</summary>
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>True when an access token is configured.</summary>
    public bool IsEnabled => !string.IsNullOrWhiteSpace(AccessToken);
}
