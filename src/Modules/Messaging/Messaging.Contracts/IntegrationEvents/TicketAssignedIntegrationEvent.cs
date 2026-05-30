using YallaJo.SharedKernel.Domain.Event;

namespace Messaging.Contracts.IntegrationEvents;

/// <summary>Logical name: messaging.ticket.assigned.v1</summary>
public sealed record TicketAssignedIntegrationEvent(
    Guid TicketId,
    Guid AssignedToUserId,
    Guid AssignedByUserId,
    DateTime AssignedAt) : IntegrationEventBase;
