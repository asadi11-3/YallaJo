namespace YallaJo.Web.Areas.Public.Models.Places;

public sealed class PlaceFiltersVm
{
    /// <summary>Exact city name (backend filters on equality, not contains).</summary>
    public string? City { get; init; }

    /// <summary>Exact country name (backend filters on equality, not contains).</summary>
    public string? Country { get; init; }

    /// <summary>Minimum average rating (0–5). Null/0 means "Any".</summary>
    public int? RatingMin { get; init; }

    /// <summary>When true, restricts to places that have at least one active tour.</summary>
    public bool HasActiveTours { get; init; }

    /// <summary>True when any filter is active (drives the filter-aware empty state).</summary>
    public bool HasAnyFilter =>
        !string.IsNullOrWhiteSpace(City)
        || !string.IsNullOrWhiteSpace(Country)
        || (RatingMin is > 0)
        || HasActiveTours;

    /// <summary>The rating value sent to the API (null when "Any"/0/out-of-range).</summary>
    public int? EffectiveRatingMin => RatingMin is >= 1 and <= 5 ? RatingMin : null;
}
