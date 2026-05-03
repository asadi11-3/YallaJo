namespace ContentTours.Presentation.Endpoints.TourPackage.Models;

public sealed record CreateTourPackageRequest(
    Guid TourId,
    string Name,
    string? Description,
    decimal Price,
    string Currency,
    int? MaxParticipants,
    DateTime? ValidFrom,
    DateTime? ValidTo
);
