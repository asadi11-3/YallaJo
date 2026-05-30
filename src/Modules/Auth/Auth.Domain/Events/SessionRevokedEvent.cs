
using YallaJo.SharedKernel.Domain.Event;

namespace Auth.Domain.Events;

public sealed record SessionRevokedEvent(
    Guid UserId,
    Guid SessionId) : DomainEventBase;
