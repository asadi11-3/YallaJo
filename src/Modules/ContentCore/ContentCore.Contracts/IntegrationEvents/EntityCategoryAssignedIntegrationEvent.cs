using YallaJo.SharedKernel.Domain.Event;

namespace ContentCore.Contracts.IntegrationEvents;

public sealed record EntityCategoryAssignedIntegrationEvent(
    string EntityType,
    Guid EntityId,
    Guid CategoryId,
    DateTime AssignedAt) : IntegrationEventBase;
