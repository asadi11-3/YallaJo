using YallaJo.SharedKernel.Domain.Event;

namespace Messaging.Domain.Events;

public sealed record NotificationReadDomainEvent(
    Guid NotificationId,
    Guid UserId,
    DateTime ReadAt) : DomainEventBase;
