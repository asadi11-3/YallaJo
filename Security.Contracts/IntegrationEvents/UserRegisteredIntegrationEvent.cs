using YallaJo.SharedKernel.Domain.Event;

namespace Security.Contracts.IntegrationEvents;

/// <summary>
/// Published when a new user is created in the Security module.
/// Consumed by any module that needs to react to user registration (e.g. Auth, Accounts).
/// </summary>
public sealed record UserRegisteredIntegrationEvent(
    Guid UserId,
    string Email) : IntegrationEventBase;
