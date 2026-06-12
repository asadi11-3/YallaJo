namespace YallaJo.Web.Infrastructure.Maps;

/// <summary>
/// View model for the shared <c>_Map</c> partial (Views/Shared/_Map.cshtml).
/// One container div per map; <c>yj-map.js</c> reads the data-* attributes and
/// lazily boots Mapbox GL only when the container nears the viewport.
/// The partial formats all numerics with the invariant culture so the Arabic UI
/// culture can never produce unparsable decimal separators.
/// </summary>
public sealed class MapVm
{
    /// <summary>Map mode consumed by yj-map.js: pin | pins | route | picker.</summary>
    public string Mode { get; init; } = "pin";

    /// <summary>Initial center / single-pin latitude.</summary>
    public decimal? Latitude { get; init; }

    /// <summary>Initial center / single-pin longitude.</summary>
    public decimal? Longitude { get; init; }

    /// <summary>Marker popup text (pin mode) and accessible label for the map.</summary>
    public string? Label { get; init; }

    /// <summary>Initial zoom (ignored when pins/route modes fit bounds).</summary>
    public int Zoom { get; init; } = 13;

    /// <summary>
    /// Pre-serialized JSON array for pins/route modes:
    /// <c>[{"lat":..,"lng":..,"label":"..","url":"..","type":"meeting"?}]</c>.
    /// </summary>
    public string? MarkersJson { get; init; }

    /// <summary>CSS selector of the latitude input to sync (picker mode).</summary>
    public string? InputLatSelector { get; init; }

    /// <summary>CSS selector of the longitude input to sync (picker mode).</summary>
    public string? InputLngSelector { get; init; }

    /// <summary>Extra classes for the container (e.g. yj-map-tall, mt-2).</summary>
    public string? CssClass { get; init; }
}
