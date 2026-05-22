using YallaJo.SharedKernel.Domain.Event;

namespace Accounts.Contracts.IntegrationEvents;

/// <summary>
/// Published (logical name: <c>accounts.provider.reinstated.v1</c>) when a suspended provider
/// is reinstated by an admin. Consumers: Security (restore provider role), Booking.
/// </summary>
public sealed record ProviderReinstatedIntegrationEvent(
    Guid ApplicationId,
    Guid UserId,
    DateTime ReinstatedAt) : IntegrationEventBase;
