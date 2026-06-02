namespace YallaJo.Web.Areas.Admin.Models.Places;

public sealed class PlaceListVm
{
    public IReadOnlyList<PlaceRowVm> Items { get; init; } = [];

    public PlaceListFilterVm Filter { get; init; } = new();

    public int  Page            { get; init; } = 1;
    public int  PageSize        { get; init; } = 20;
    public int  TotalCount      { get; init; }
    public int  TotalPages      { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage     { get; init; }
}
