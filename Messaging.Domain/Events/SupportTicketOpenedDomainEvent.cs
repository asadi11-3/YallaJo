using Messaging.Domain.Enums;
using YallaJo.SharedKernel.Domain.Event;

namespace Messaging.Domain.Events;

public sealed record SupportTicketOpenedDomainEvent(
    Guid TicketId,
    Guid UserId,
    TicketPriority Priority,
    string Subject) : DomainEventBase;
