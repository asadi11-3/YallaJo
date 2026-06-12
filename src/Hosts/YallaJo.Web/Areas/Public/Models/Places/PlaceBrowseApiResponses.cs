namespace YallaJo.Web.Areas.Public.Models.Places;

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

public sealed class PlaceSummaryResponse
{
    public Guid    Id            { get; init; }
    public string  Name          { get; init; } = string.Empty;
    public string  Slug          { get; init; } = string.Empty;
    public string  PlaceType     { get; init; } = string.Empty;
    public decimal Latitude      { get; init; }
    public decimal Longitude     { get; init; }
    public string? City          { get; init; }
    public string? Country       { get; init; }
    public decimal AverageRating { get; init; }
    public int     ReviewCount   { get; init; }
    public bool    IsFeatured    { get; init; }
    public bool    IsVerified    { get; init; }
}

public sealed class PlaceDetailResponse
{
    public Guid    Id                     { get; init; }
    public string  Name                   { get; init; } = string.Empty;
    public string  Slug                   { get; init; } = string.Empty;
    public string  PlaceType              { get; init; } = string.Empty;
    public decimal Latitude               { get; init; }
    public decimal Longitude              { get; init; }
    public string? Description            { get; init; }
    public string? Address                { get; init; }
    public string? City                   { get; init; }
    public string? Country                { get; init; }
    public string? PostalCode             { get; init; }
    public string? Phone                  { get; init; }
    public string? Email                  { get; init; }
    public string? Website                { get; init; }
    public decimal AverageRating          { get; init; }
    public int     ReviewCount            { get; init; }
    public bool    IsFeatured             { get; init; }
    public bool    IsVerified             { get; init; }
    public bool    IsWheelchairAccessible { get; init; }
    public bool    HasAudioGuide          { get; init; }
    public bool    HasBrailleSignage      { get; init; }
    public string? MetaTitle              { get; init; }
    public string? MetaDescription        { get; init; }
}
