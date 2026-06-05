namespace YallaJo.Web.Areas.Guide.Models.Proposals;

/// <summary>
/// A tour proposal as returned by GET /api/v1/tours/proposals.
/// NOTE: the backend list endpoint is currently a stub that returns an empty
/// array, so this shape is forward-looking and never actually populated yet.
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
