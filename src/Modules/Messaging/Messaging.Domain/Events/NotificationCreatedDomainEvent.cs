using Messaging.Domain.Enums;
using YallaJo.SharedKernel.Domain.Event;

namespace Messaging.Domain.Events;

public sealed record NotificationCreatedDomainEvent(
    Guid NotificationId,
    Guid UserId,
    NotificationType Type,
    NotificationChannel Channel,
    NotificationPriority Priority) : DomainEventBase;
