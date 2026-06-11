namespace ContentTours.Application.Queries.Tour.GetMyTourStatusCounts;

/// <summary>
/// [Backend] B1 — aggregate counts keyed by <c>TourStatus</c> names, aligned with the
/// <c>status</c> filter values accepted by GET /tours/provider/my-tours.
/// </summary>
public sealed record TourStatusCountsDto(
    int Draft,
    int Pending,
    int Approved,
    int Rejected,
    int Suspended,
    int Archived,
    int Total);
