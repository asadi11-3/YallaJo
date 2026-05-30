using YallaJo.SharedKernel.Domain.Event;

namespace Messaging.Contracts.IntegrationEvents;

/// <summary>Logical name: messaging.ticket.created.v1</summary>
public sealed record TicketCreatedIntegrationEvent(
    Guid TicketId,
    Guid CreatedByUserId,
    string Category,
    string Priority,
    string Subject,
    DateTime SlaBreachAt,
    DateTime CreatedAt) : IntegrationEventBase;
