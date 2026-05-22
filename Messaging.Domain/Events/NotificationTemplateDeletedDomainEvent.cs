using YallaJo.SharedKernel.Domain.Event;

namespace Messaging.Domain.Events;

/// <summary>Raised when a notification template is soft-deleted.</summary>
public sealed record NotificationTemplateDeletedDomainEvent(
    Guid TemplateId) : DomainEventBase;
