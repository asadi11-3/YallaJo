// Security.Domain/Events/PasswordChangedEvent.cs
using YallaJo.SharedKernel.Domain.Event;

namespace Security.Domain.Events;

/// <summary>
/// Raised when a user changes their password via ChangePassword command.
/// Consumed by Auth module to revoke all active sessions and refresh tokens,
/// ensuring compromised sessions cannot survive a password change.
/// </summary>
public sealed record PasswordChangedEvent(Guid UserId) : DomainEventBase;
