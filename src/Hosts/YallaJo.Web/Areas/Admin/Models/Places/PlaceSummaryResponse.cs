using YallaJo.Web.Areas.Admin.Models.Places;

namespace YallaJo.Web.Areas.Admin.Models.Places;

public sealed class PlaceSummaryResponse
{
    public Guid             Id            { get; init; }
    public string           Name          { get; init; } = string.Empty;
    public string           Slug          { get; init; } = string.Empty;
    // The API serializes enums as their string name (e.g. "Historical") via
    // a global JsonStringEnumConverter. The Web BFF's response deserializer is
    // NOT enum-aware, so this is received as a string — matching the project
    // convention (every other Web response DTO types enum fields as string).
    public string           PlaceType     { get; init; } = string.Empty;
    public decimal          Latitude      { get; init; }
    public decimal          Longitude     { get; init; }
    public string?          City          { get; init; }
    public string?          Country       { get; init; }
    public decimal          AverageRating { get; init; }
    public int              ReviewCount   { get; init; }
    public bool             IsFeatured    { get; init; }
    public bool             IsVerified    { get; init; }
}
