using YallaJo.SharedKernel.Domain.Event;

namespace Social.Domain.Events;

/// <summary>Raised when an admin closes a report with an action decision.</summary>
public sealed record ReportResolvedDomainEvent(
    Guid ReportId, Guid AdminUserId,
    Social.Domain.Enums.ModerationAction Action, DateTime ResolvedAt) : DomainEventBase;
