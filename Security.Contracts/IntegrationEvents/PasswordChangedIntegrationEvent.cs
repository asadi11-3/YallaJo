// Security.Contracts/IntegrationEvents/PasswordChangedIntegrationEvent.cs
using YallaJo.SharedKernel.Domain.Event;

namespace Security.Contracts.IntegrationEvents;

/// <summary>
/// Published when a user successfully changes their password.
/// Consumed by Auth module to revoke all active sessions and refresh tokens.
/// </summary>
public sealed record PasswordChangedIntegrationEvent(
    Guid UserId) : IntegrationEventBase;
