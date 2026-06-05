namespace YallaJo.Web.Areas.Public.Models.Places;

/// <summary>A single place card on the public browse grid.</summary>
public sealed class PlaceCardVm
{
    public Guid    Id            { get; init; }
    public string  Name          { get; init; } = string.Empty;
    public string  Slug          { get; init; } = string.Empty;
    public string? ImageUrl      { get; init; }
    public string  PlaceType     { get; init; } = string.Empty;
    public string? City          { get; init; }
    public string? Country       { get; init; }
    public decimal AverageRating { get; init; }
    public int     ReviewCount   { get; init; }
    public bool    IsFeatured    { get; init; }
    public bool    IsVerified    { get; init; }

    /// <summary>"City, Country" / "City" / "Country" / null when both missing.</summary>
    public string? LocationLabel => FormatLocation(City, Country);

    internal static string? FormatLocation(string? city, string? country) =>
        (city?.Trim(), country?.Trim()) switch
        {
            ({ Length: > 0 } c, { Length: > 0 } co) => $"{c}, {co}",
            ({ Length: > 0 } c, _)                   => c,
            (_, { Length: > 0 } co)                  => co,
            _                                        => null,
        };
}

/// <summary>The public places browse grid (list + pagination + echoed filters).</summary>
public sealed class PlacesGridVm
{
    public IReadOnlyList<PlaceCardVm> Places { get; init; } = [];

    public int  PageNumber      { get; init; } = 1;
    public int  PageSize        { get; init; } = 20;
    public int  TotalCount      { get; init; }
    public int  TotalPages      { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage     { get; init; }

    /// <summary>Echoed filters — keeps the form populated and pagination links filter-aware.</summary>
    public PlaceFiltersVm Filters { get; init; } = new();

    public bool HasResults => Places.Count > 0;
}

/// <summary>The public place detail page.</summary>
public sealed class PlaceDetailVm
{
    public Guid    Id                     { get; init; }
    public string  Name                   { get; init; } = string.Empty;
    public string  Slug                   { get; init; } = string.Empty;
    public string? ImageUrl               { get; init; }
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

    public string? LocationLabel => PlaceCardVm.FormatLocation(City, Country);
    public bool HasContact => !string.IsNullOrWhiteSpace(Phone)
                              || !string.IsNullOrWhiteSpace(Email)
                              || !string.IsNullOrWhiteSpace(Website);
    public bool HasAccessibility => IsWheelchairAccessible || HasAudioGuide || HasBrailleSignage;
}
