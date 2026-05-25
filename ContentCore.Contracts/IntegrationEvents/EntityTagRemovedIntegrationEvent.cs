using YallaJo.SharedKernel.Domain.Event;

namespace ContentCore.Contracts.IntegrationEvents;

public sealed record EntityTagRemovedIntegrationEvent(
    string EntityType,
    Guid EntityId,
    Guid TagId,
    DateTime RemovedAt) : IntegrationEventBase;
