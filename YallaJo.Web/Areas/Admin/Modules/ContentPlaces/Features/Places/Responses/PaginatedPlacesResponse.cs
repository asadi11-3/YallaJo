namespace YallaJo.Web.Areas.Admin.Modules.ContentPlaces.Features.Places.Responses;

public sealed class PaginatedPlacesResponse
{
    public List<PlaceSummaryResponse> Items           { get; init; } = [];
    public int                        PageNumber      { get; init; }
    public int                        PageSize        { get; init; }
    public int                        TotalCount      { get; init; }
    public int                        TotalPages      { get; init; }
    public bool                       HasPreviousPage { get; init; }
    public bool                       HasNextPage     { get; init; }
}
