using YallaJo.SharedKernel.Domain.Event;

namespace Accounts.Contracts.IntegrationEvents;

/// <summary>
/// Published (logical name: <c>accounts.provider.status-changed.v1</c>) on every status
/// transition for general-purpose consumers (analytics, audit trail).
/// </summary>
public sealed record ProviderStatusChangedIntegrationEvent(
    Guid ApplicationId,
    Guid UserId,
    string PreviousStatus,
    string NewStatus,
    DateTime ChangedAt) : IntegrationEventBase;
