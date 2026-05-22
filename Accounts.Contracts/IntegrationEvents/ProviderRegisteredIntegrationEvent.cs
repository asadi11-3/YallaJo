using YallaJo.SharedKernel.Domain.Event;

namespace Accounts.Contracts.IntegrationEvents;

/// <summary>
/// Published (logical name: <c>accounts.provider.registered.v1</c>) when a user registers
/// a new provider application (Draft status). Consumers: Messaging (welcome email).
/// </summary>
public sealed record ProviderRegisteredIntegrationEvent(
    Guid ApplicationId,
    Guid UserId,
    string ProviderType,
    DateTime RegisteredAt) : IntegrationEventBase;
