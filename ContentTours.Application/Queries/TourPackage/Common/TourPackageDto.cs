namespace ContentTours.Application.Queries.TourPackage.Common;

public sealed record TourPackageDto(
    Guid Id,
    Guid TourId,
    string Name,
    string? Description,
    decimal PriceAmount,
    string Currency,
    int? MaxParticipants,
    DateTime? ValidFrom,
    DateTime? ValidTo,
    bool IsActive,
    DateTime CreatedAt
);
