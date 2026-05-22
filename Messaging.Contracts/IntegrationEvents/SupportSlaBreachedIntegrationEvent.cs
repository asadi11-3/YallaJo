using YallaJo.SharedKernel.Domain.Event;

namespace Messaging.Contracts.IntegrationEvents;

/// <summary>Logical name: messaging.support-sla-breached.v1</summary>
public sealed record SupportSlaBreachedIntegrationEvent(
    Guid TicketId,
    Guid CreatedByUserId,
    Guid? AssignedToUserId,
    string Priority,
    DateTime SlaBreachAt,
    DateTime DetectedAt) : IntegrationEventBase;
