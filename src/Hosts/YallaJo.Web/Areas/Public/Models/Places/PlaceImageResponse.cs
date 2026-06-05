namespace YallaJo.Web.Areas.Public.Models.Places;

/// <summary>
/// Mirrors the public API <c>PlaceImageDto</c> from
/// <c>GET /api/v1/places/{id}/images</c> (CP-4, public-safe). No enum fields,
/// no internal attachment metadata.
/// </summary>
public sealed class PlaceImageResponse
{
    public string  Url          { get; init; } = string.Empty;
    public string? ThumbnailUrl { get; init; }
    public int     SortOrder    { get; init; }
    public bool    IsPrimary    { get; init; }
}
