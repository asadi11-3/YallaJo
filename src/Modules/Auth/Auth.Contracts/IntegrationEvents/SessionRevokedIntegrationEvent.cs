
using YallaJo.SharedKernel.Domain.Event;

namespace Auth.Contracts.IntegrationEvents;


public sealed record SessionRevokedIntegrationEvent(
    Guid UserId,
    Guid SessionId) : IntegrationEventBase;
