using YallaJo.SharedKernel.Domain.Event;

namespace Security.Contracts.IntegrationEvents;

/// <summary>
/// Published when a new user is created in the Security module.
/// Consumed by modules that need identity bootstrap behavior (e.g. Auth, Accounts).
/// </summary>
public sealed record UserCreatedIntegrationEvent(
    Guid UserId,
    string Email) : IntegrationEventBase;
