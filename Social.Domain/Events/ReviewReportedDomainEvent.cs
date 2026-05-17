using YallaJo.SharedKernel.Domain.Event;

namespace Social.Domain.Events;

public sealed record ReviewReportedDomainEvent(Guid ReviewId, Guid ReporterUserId, int ReportCount) : DomainEventBase;
