using YallaJo.SharedKernel.Domain.Event;

namespace ContentCore.Contracts.IntegrationEvents;

public sealed record EntityCategoryRemovedIntegrationEvent(
    string EntityType,
    Guid EntityId,
    Guid CategoryId,
    DateTime RemovedAt) : IntegrationEventBase;
