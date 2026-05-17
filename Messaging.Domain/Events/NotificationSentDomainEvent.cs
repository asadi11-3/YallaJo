using Messaging.Domain.Enums;
using YallaJo.SharedKernel.Domain.Event;

namespace Messaging.Domain.Events;

public sealed record NotificationSentDomainEvent(
    Guid NotificationId,
    Guid UserId,
    NotificationChannel Channel) : DomainEventBase;
