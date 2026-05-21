using YallaJo.SharedKernel.Domain.Event;

namespace Social.Domain.Events;

/// <summary>Raised when a user removes an entity from their favorites list.</summary>
public sealed record FavoriteRemovedDomainEvent(
    Guid FavoriteId, Guid UserId,
    Social.Domain.Enums.FavoriteEntityType EntityType, Guid EntityId,
    DateTime RemovedAt) : DomainEventBase;
