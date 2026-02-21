using YallaJo.SharedKernel.Domain.Event;

namespace Accounts.Contracts.IntegrationEvents;

/// <summary>
/// Published when a new user registers in the Accounts module.
/// Consumed by any module that needs to react to user registration (e.g. Auth).
/// </summary>
public sealed record UserRegisteredIntegrationEvent(
    Guid UserId,
    string Email) : IntegrationEventBase;
