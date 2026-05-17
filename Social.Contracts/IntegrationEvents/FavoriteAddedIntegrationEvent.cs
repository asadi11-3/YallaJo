using YallaJo.SharedKernel.Domain.Event;

namespace Social.Contracts.IntegrationEvents;

public sealed record FavoriteAddedIntegrationEvent(Guid FavoriteId, Guid UserId, string EntityType, Guid EntityId) : IntegrationEventBase;
