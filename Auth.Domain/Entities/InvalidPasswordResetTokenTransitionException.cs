using YallaJo.SharedKernel.Domain.Exceptions;

namespace Auth.Domain.Entities;

/// <summary>
/// Thrown when a caller attempts to move a <see cref="PasswordResetToken"/>
/// through a transition that the state machine does not permit (for
/// example, consuming a revoked token or marking an already-consumed
/// token as delivered).
/// <para>
/// Escapes the aggregate as a programmer-error signal. Application
/// handlers should return <c>Result.Failure</c> for user-facing invalid
/// token conditions (expired, unknown, hash mismatch, attempt budget
/// exceeded) BEFORE invoking the domain verb — this exception exists as
/// the last-line invariant guard.
/// </para>
/// </summary>
public sealed class InvalidPasswordResetTokenTransitionException(
    PasswordResetTokenState from,
    PasswordResetTokenState to)
    : DomainException(
        code: "INVALID_PASSWORD_RESET_TOKEN_TRANSITION",
        message: $"Password reset token transition not allowed: {from} → {to}.")
{
    public PasswordResetTokenState From { get; } = from;
    public PasswordResetTokenState To   { get; } = to;
}
