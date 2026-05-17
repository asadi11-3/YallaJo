using YallaJo.SharedKernel.Domain.Event;

namespace Social.Contracts.IntegrationEvents;

public sealed record ContentHiddenIntegrationEvent(Guid LogId, string EntityType, Guid EntityId, Guid ModeratorUserId, string? Reason) : IntegrationEventBase;
