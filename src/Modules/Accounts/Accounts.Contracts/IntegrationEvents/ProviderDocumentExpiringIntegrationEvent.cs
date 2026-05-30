using YallaJo.SharedKernel.Domain.Event;

namespace Accounts.Contracts.IntegrationEvents;

/// <summary>
/// Published (logical name: <c>accounts.provider.document-expiring.v1</c>) when a provider's
/// document is expiring within the configured threshold (default 30 days).
/// Consumers: Messaging (send notification to provider).
/// </summary>
public sealed record ProviderDocumentExpiringIntegrationEvent(
    Guid ApplicationId,
    Guid UserId,
    string DocumentType,
    string DocumentFileName,
    DateTime ExpiresAt,
    int DaysUntilExpiry) : IntegrationEventBase;
