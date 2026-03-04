using YallaJo.SharedKernel.Domain.Event;

namespace Security.Contracts.IntegrationEvents;

/// <summary>
/// Published when a user successfully resets their password.
/// Consumed by Auth module to revoke all active sessions and refresh tokens.
/// </summary>
public sealed record PasswordResetIntegrationEvent(
    Guid UserId) : IntegrationEventBase;
