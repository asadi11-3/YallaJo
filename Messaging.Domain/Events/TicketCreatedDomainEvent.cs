using Messaging.Domain.Enums;
using YallaJo.SharedKernel.Domain.Event;

namespace Messaging.Domain.Events;

/// <summary>Raised when a support ticket is created by a user.</summary>
public sealed record TicketCreatedDomainEvent(
    Guid TicketId,
    Guid CreatedByUserId,
    TicketCategory Category,
    TicketPriority Priority,
    string Subject,
    DateTime SlaBreachAt) : DomainEventBase;
