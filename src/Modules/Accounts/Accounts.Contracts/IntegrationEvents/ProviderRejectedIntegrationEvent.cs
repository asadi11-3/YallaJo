using YallaJo.SharedKernel.Domain.Event;

namespace Accounts.Contracts.IntegrationEvents;

/// <summary>
/// Published (logical name: <c>accounts.provider.rejected.v1</c>) when an admin rejects
/// a provider application. Consumers: Messaging (notification email).
/// </summary>
public sealed record ProviderRejectedIntegrationEvent(
    Guid ApplicationId,
    Guid UserId,
    string Reason,
    DateTime RejectedAt) : IntegrationEventBase;
