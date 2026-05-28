using YallaJo.SharedKernel.Domain.Event;

namespace ContentCore.Contracts.IntegrationEvents;

public sealed record EntityTagAssignedIntegrationEvent(
    string EntityType,
    Guid EntityId,
    Guid TagId,
    DateTime AssignedAt) : IntegrationEventBase;
