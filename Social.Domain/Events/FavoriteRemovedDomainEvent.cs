using YallaJo.SharedKernel.Domain.Event;

namespace Social.Domain.Events;

public sealed record FavoriteRemovedDomainEvent(Guid FavoriteId, Guid UserId, string EntityType, Guid EntityId) : DomainEventBase;
