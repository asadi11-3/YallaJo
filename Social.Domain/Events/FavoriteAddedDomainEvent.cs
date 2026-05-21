using YallaJo.SharedKernel.Domain.Event;

namespace Social.Domain.Events;

/// <summary>Raised when a user adds an entity to their favorites list.</summary>
public sealed record FavoriteAddedDomainEvent(
    Guid FavoriteId, Guid UserId,
    Social.Domain.Enums.FavoriteEntityType EntityType, Guid EntityId,
    DateTime AddedAt) : DomainEventBase;
