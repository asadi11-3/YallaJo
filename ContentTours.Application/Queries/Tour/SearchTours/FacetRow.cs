namespace ContentTours.Application.Queries.Tour.SearchTours;

/// <summary>Lightweight projection for facet computation (capped at 5000 rows).</summary>
internal sealed record FacetRow(
    decimal BasePrice,
    string Difficulty,
    decimal AverageRating,
    bool IsChildFriendly,
    bool IsAccessible,
    bool IsInstantBooking);
