using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Contracts.IntegrationEvents;

public sealed record TourPackageUpdatedIntegrationEvent(
    Guid PackageId,
    Guid UpdatedByUserId,
    string Name,
    decimal Price,
    string Currency,
    int? MaxParticipants,
    DateTime? ValidFrom,
    DateTime? ValidTo,
    IReadOnlyCollection<Guid> IncludedTourIds,
    IReadOnlyCollection<Guid> PreviousIncludedTourIds
) : IntegrationEventBase;
