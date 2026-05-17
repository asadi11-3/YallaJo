using YallaJo.SharedKernel.Domain.Event;

namespace Social.Contracts.IntegrationEvents;

public sealed record ReviewDeletedIntegrationEvent(Guid ReviewId, Guid UserId, string Reason) : IntegrationEventBase;
