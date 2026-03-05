// Auth.Contracts/IntegrationEvents/SessionRevokedIntegrationEvent.cs
using YallaJo.SharedKernel.Domain.Event;

namespace Auth.Contracts.IntegrationEvents;

/// <summary>
/// Published when a session is revoked (logout, password change, forced expiry).
/// Consumed by modules that need to react to session termination (e.g. AuditLog).
/// </summary>
public sealed record SessionRevokedIntegrationEvent(
    Guid UserId,
    Guid SessionId) : IntegrationEventBase;
