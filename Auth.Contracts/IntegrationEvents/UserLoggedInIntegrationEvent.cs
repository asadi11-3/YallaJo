// Auth.Contracts/IntegrationEvents/UserLoggedInIntegrationEvent.cs
using YallaJo.SharedKernel.Domain.Event;

namespace Auth.Contracts.IntegrationEvents;

/// <summary>
/// Published when a user successfully authenticates and a new session is created.
/// Consumed by modules that need to react to login activity (e.g. AuditLog).
/// </summary>
public sealed record UserLoggedInIntegrationEvent(
    Guid UserId,
    Guid SessionId,
    Guid DeviceId,
    string IpAddress) : IntegrationEventBase;
