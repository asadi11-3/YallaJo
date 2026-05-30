namespace ContentTours.Presentation.Endpoints.TourGuide.Models;

internal sealed record CreateTourProposalRequest(
    string Title,
    string Description,
    string? ShortDescription,
    Guid PlaceId,
    int DurationMinutes,
    int MaxGroupSize,
    decimal BasePrice,
    string Currency,
    bool RequestExclusive = false);

internal sealed record UpdateTourProposalRequest(
    string Title,
    string Description,
    string? ShortDescription,
    Guid PlaceId,
    int DurationMinutes,
    int MaxGroupSize,
    decimal BasePrice,
    string Currency,
    bool RequestExclusive = false);

internal sealed record RejectTourProposalRequest(string Reason);

internal sealed record ApproveTourProposalRequest(bool IsExclusive = false);
