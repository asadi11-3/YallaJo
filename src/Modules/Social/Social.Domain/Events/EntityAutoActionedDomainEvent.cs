using YallaJo.SharedKernel.Domain.Event;

namespace Social.Domain.Events;

/// <summary>Raised when an entity is automatically actioned due to accumulating reports.</summary>
public sealed record EntityAutoActionedDomainEvent(
    Social.Domain.Enums.ReportableEntityType EntityType, Guid EntityId,
    int ReportCount, DateTime ActionedAt) : DomainEventBase;
