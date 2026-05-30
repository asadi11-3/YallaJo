namespace ContentTours.Presentation.Endpoints.TourPackage.Models;

public sealed record CreateTourPackageRequest(
    string Name,
    string? Description,
    decimal Price,
    string Currency,
    int? MaxParticipants,
    DateTime? ValidFrom,
    DateTime? ValidTo,
    IReadOnlyCollection<Guid> IncludedTourIds,
    IReadOnlyCollection<string> Inclusions);
