using Social.Domain.Enums;
using YallaJo.SharedKernel.Domain.Event;

namespace Social.Domain.Events;

public sealed record ReportResolvedDomainEvent(Guid ReportId, Guid ResolvedByUserId, ReportStatus Status, string? ResolutionNotes) : DomainEventBase;
