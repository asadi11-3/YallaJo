namespace ContentTours.Application.Queries.TourPackage.Common;

public sealed record TourPackageSummaryDto(
    Guid Id,
    Guid CreatedByUserId,
    string Name,
    string? Description,
    decimal PriceAmount,
    string Currency,
    int? MaxParticipants,
    DateTime? ValidFrom,
    DateTime? ValidTo,
    int IncludedTourCount,
    DateTime CreatedAt);
