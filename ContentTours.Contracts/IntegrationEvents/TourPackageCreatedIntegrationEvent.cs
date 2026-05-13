using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Contracts.IntegrationEvents;

public sealed record TourPackageCreatedIntegrationEvent(
    Guid PackageId,
    Guid CreatedByUserId,
    string Name,
    decimal Price,
    string Currency,
    int? MaxParticipants,
    DateTime? ValidFrom,
    DateTime? ValidTo,
    IReadOnlyCollection<Guid> IncludedTourIds
) : IntegrationEventBase;
