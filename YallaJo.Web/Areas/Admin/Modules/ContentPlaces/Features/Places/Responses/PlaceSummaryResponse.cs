using YallaJo.Web.Areas.Admin.Modules.ContentPlaces.Features.Places.ViewModels;

namespace YallaJo.Web.Areas.Admin.Modules.ContentPlaces.Features.Places.Responses;

public sealed class PlaceSummaryResponse
{
    public Guid             Id            { get; init; }
    public string           Name          { get; init; } = string.Empty;
    public string           Slug          { get; init; } = string.Empty;
    public PlaceTypeOption  PlaceType     { get; init; }
    public decimal          Latitude      { get; init; }
    public decimal          Longitude     { get; init; }
    public string?          City          { get; init; }
    public string?          Country       { get; init; }
    public decimal          AverageRating { get; init; }
    public int              ReviewCount   { get; init; }
    public bool             IsFeatured    { get; init; }
    public bool             IsVerified    { get; init; }
}
