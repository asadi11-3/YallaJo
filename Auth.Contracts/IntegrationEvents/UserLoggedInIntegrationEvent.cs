
using YallaJo.SharedKernel.Domain.Event;

namespace Auth.Contracts.IntegrationEvents;


public sealed record UserLoggedInIntegrationEvent(
    Guid UserId,
    Guid SessionId,
    Guid DeviceId,
    string IpAddress) : IntegrationEventBase;
