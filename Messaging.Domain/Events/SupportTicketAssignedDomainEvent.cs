using YallaJo.SharedKernel.Domain.Event;

namespace Messaging.Domain.Events;

public sealed record SupportTicketAssignedDomainEvent(
    Guid TicketId,
    Guid AssignedToUserId) : DomainEventBase;
