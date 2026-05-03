namespace ContentTours.Presentation.Endpoints.TourPackage.Models;

public sealed record UpdateTourPackageRequest(
    string Name,
    string? Description,
    decimal Price,
    string Currency,
    int? MaxParticipants,
    DateTime? ValidTo
);
