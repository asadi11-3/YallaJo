using Messaging.Domain.Enums;
using YallaJo.SharedKernel.Domain.Event;

namespace Messaging.Domain.Events;

/// <summary>Raised when a notification is successfully delivered to the channel.</summary>
public sealed record NotificationDeliveredDomainEvent(
    Guid NotificationId,
    Guid UserId,
    NotificationChannel Channel,
    DateTime DeliveredAt) : DomainEventBase;
