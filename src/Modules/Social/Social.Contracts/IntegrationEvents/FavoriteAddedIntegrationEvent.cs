using YallaJo.SharedKernel.Domain.Event;

namespace Social.Contracts.IntegrationEvents;

/// <summary>social.favorite.added.v1 — emitted when a user adds an entity to favorites.</summary>
public sealed record FavoriteAddedIntegrationEvent(
    Guid FavoriteId, Guid UserId, string EntityType, Guid EntityId,
    DateTime AddedAt) : IntegrationEventBase;
