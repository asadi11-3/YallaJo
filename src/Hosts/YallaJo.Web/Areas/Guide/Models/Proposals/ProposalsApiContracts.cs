namespace YallaJo.Web.Areas.Guide.Models.Proposals;

/// <summary>
/// A tour proposal as returned by GET /api/v1/tours/proposals.
/// </summary>
public sealed record TourProposalResponse(
    Guid Id,
    string Title,
    string? ShortDescription,
    string Status,
    decimal BasePrice,
    string Currency,
    DateTime CreatedAt);

/// <summary>
/// POST /api/v1/tours/proposals — create a draft proposal.
/// </summary>
public sealed record CreateTourProposalRequest(
    string Title,
    string Description,
    string? ShortDescription,
    Guid PlaceId,
    int DurationMinutes,
    int MaxGroupSize,
    decimal BasePrice,
    string Currency,
    bool RequestExclusive = false);

/// <summary>GET /api/v1/places/lookup — mirrors ContentPlaces PlaceLookupDto.</summary>
public sealed record PlaceLookupResponse(Guid Id, string Name, string? City);
