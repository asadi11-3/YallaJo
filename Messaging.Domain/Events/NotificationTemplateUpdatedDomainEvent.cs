using YallaJo.SharedKernel.Domain.Event;

namespace Messaging.Domain.Events;

public sealed record NotificationTemplateUpdatedDomainEvent(
    Guid TemplateId,
    string Code) : DomainEventBase;
