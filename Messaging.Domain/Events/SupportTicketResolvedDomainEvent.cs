using YallaJo.SharedKernel.Domain.Event;

namespace Messaging.Domain.Events;

public sealed record SupportTicketResolvedDomainEvent(
    Guid TicketId,
    Guid ResolvedByUserId,
    DateTime ResolvedAt,
    string? ResolutionNotes) : DomainEventBase;
