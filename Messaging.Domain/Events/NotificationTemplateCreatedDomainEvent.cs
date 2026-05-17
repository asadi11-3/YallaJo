using Messaging.Domain.Enums;
using YallaJo.SharedKernel.Domain.Event;

namespace Messaging.Domain.Events;

public sealed record NotificationTemplateCreatedDomainEvent(
    Guid TemplateId,
    string Code,
    NotificationType Type,
    NotificationChannel Channel) : DomainEventBase;
