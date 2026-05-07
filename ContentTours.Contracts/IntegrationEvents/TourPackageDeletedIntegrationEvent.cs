using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Contracts.IntegrationEvents;

public sealed record TourPackageDeletedIntegrationEvent(
    Guid PackageId,
    Guid DeletedByUserId,
    IReadOnlyCollection<Guid> IncludedTourIds
) : IntegrationEventBase;
