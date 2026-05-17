using Social.Domain.Enums;
using YallaJo.SharedKernel.Domain.Event;

namespace Social.Domain.Events;

public sealed record ReportCreatedDomainEvent(Guid ReportId, Guid ReporterUserId, string EntityType, Guid EntityId, ReportReason Reason) : DomainEventBase;
