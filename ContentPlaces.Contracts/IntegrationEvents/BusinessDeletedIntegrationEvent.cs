using YallaJo.SharedKernel.Domain.Event;

namespace ContentPlaces.Contracts.IntegrationEvents;

public sealed record BusinessDeletedIntegrationEvent(
    Guid BusinessId,
    DateTime DeletedAt) : IntegrationEventBase;
