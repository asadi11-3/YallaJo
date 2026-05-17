using YallaJo.SharedKernel.Domain.Event;

namespace Messaging.Domain.Events;

public sealed record DeviceTokenRevokedDomainEvent(
    Guid DeviceTokenId,
    Guid UserId) : DomainEventBase;
