using YallaJo.SharedKernel.Domain.Event;

namespace Accounts.Contracts.IntegrationEvents;

/// <summary>
/// Published (logical name: <c>accounts.provider.suspended.v1</c>) when an approved provider
/// is suspended. Consumers: Booking (cancel active bookings), Finance.
/// </summary>
public sealed record ProviderSuspendedIntegrationEvent(
    Guid ApplicationId,
    Guid UserId,
    string Reason,
    DateTime SuspendedAt) : IntegrationEventBase;
