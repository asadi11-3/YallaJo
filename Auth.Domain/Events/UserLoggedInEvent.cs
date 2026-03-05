
using YallaJo.SharedKernel.Domain.Event;

namespace Auth.Domain.Events;

public sealed record UserLoggedInEvent(
    Guid UserId,
    Guid SessionId,
    Guid DeviceId,
    string IpAddress) : DomainEventBase;
