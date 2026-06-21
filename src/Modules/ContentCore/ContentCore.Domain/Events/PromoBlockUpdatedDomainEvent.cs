using YallaJo.SharedKernel.Domain.Event;

namespace ContentCore.Domain.Events;

public sealed record PromoBlockUpdatedDomainEvent(
    Guid PromoBlockId,
    string PlacementKey) : DomainEventBase;
