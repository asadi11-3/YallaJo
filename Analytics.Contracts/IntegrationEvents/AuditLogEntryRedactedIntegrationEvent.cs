using YallaJo.SharedKernel.Domain.Event;

namespace Analytics.Contracts.IntegrationEvents;

public sealed record AuditLogEntryRedactedIntegrationEvent(
    long EntryId,
    string EntityType,
    Guid EntityId,
    string RedactedByAdminId,
    DateTime RedactedAt) : IntegrationEventBase;
