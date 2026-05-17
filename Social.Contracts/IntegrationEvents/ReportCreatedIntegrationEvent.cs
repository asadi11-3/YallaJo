using YallaJo.SharedKernel.Domain.Event;

namespace Social.Contracts.IntegrationEvents;

public sealed record ReportCreatedIntegrationEvent(Guid ReportId, Guid ReporterUserId, string EntityType, Guid EntityId, string Reason) : IntegrationEventBase;
