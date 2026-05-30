using Messaging.Domain.Enums;
using YallaJo.SharedKernel.Domain.Event;

namespace Messaging.Domain.Events;

public sealed record NotificationFailedDomainEvent(
    Guid NotificationId,
    Guid UserId,
    NotificationChannel Channel,
    string FailureReason) : DomainEventBase;
