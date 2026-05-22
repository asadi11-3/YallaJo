using YallaJo.SharedKernel.Domain.Event;

namespace Accounts.Contracts.IntegrationEvents;

/// <summary>
/// Published (logical name: <c>accounts.provider.approved.v1</c>) when an admin approves
/// a provider application. Consumers: Security (grant provider role), ContentTours, Booking, Finance.
/// </summary>
public sealed record ProviderApprovedIntegrationEvent(
    Guid ApplicationId,
    Guid UserId,
    string ProviderType,
    DateTime ApprovedAt,
    Guid ApprovedByAdminId) : IntegrationEventBase;
