using ContentTours.Application.Queries.Tour.Common;

namespace ContentTours.Application.Queries.TourPackage.Common;

public sealed record TourPackageDetailDto(
    Guid Id,
    string Name,
    string? Description,
    decimal PriceAmount,
    string Currency,
    int? MaxParticipants,
    DateTime? ValidFrom,
    DateTime? ValidTo,
    bool IsActive,
    DateTime CreatedAt,
    IReadOnlyList<TourSummaryDto> IncludedTours,
    IReadOnlyList<TourPackageInclusionDto> Inclusions);
