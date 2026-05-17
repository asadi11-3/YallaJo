using YallaJo.SharedKernel.Domain.Event;

namespace Messaging.Domain.Events;

public sealed record TicketMessagePostedDomainEvent(
    Guid TicketId,
    Guid SenderUserId,
    bool IsStaffReply) : DomainEventBase;
