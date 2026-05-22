using Messaging.Domain.Enums;
using YallaJo.SharedKernel.Domain.Event;

namespace Messaging.Domain.Events;

public sealed record NotificationTemplateCreatedDomainEvent(
    Guid TemplateId,
    NotificationType Type,
    NotificationChannel Channel,
    string LanguageCode) : DomainEventBase;
