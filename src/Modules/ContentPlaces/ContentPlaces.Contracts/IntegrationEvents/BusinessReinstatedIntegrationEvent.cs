using YallaJo.SharedKernel.Domain.Event;

namespace ContentPlaces.Contracts.IntegrationEvents;

public sealed record BusinessReinstatedIntegrationEvent(
    Guid BusinessId,
    Guid OwnerId) : IntegrationEventBase;
