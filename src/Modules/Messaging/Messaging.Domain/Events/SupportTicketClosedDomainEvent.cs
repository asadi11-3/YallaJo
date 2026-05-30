using YallaJo.SharedKernel.Domain.Event;

namespace Messaging.Domain.Events;

public sealed record SupportTicketClosedDomainEvent(
    Guid TicketId,
    Guid ClosedByUserId) : DomainEventBase;
