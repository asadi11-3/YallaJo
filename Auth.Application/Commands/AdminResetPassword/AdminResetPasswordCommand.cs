using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Application.Commands.AdminResetPassword;

/// <summary>
/// Phase 3A — admin-initiated forced password reset. Issues a
/// <c>PasswordResetToken</c> with
/// <c>PasswordResetOrigin.AdminInitiated</c>, transitions the target
/// user to <c>PendingPasswordReset</c>, revokes all active sessions +
/// refresh tokens, and queues the reset email via the existing outbox
/// dispatch pipeline.
/// <para>
/// The admin does NOT replace the password directly — the user
/// completes the reset via the emailed code and
/// <c>ResetPasswordCommand</c>. On successful completion the user is
/// returned to <c>Active</c> atomically with the password mutation
/// (handled inside <c>ISecurityService.ReplacePasswordBySelfAsync</c>).
/// </para>
/// <para>
/// The actor identity is resolved from <c>ICurrentUser</c> inside the
/// handler — callers do NOT supply it on the command, so the
/// admin-cannot-forge-actor invariant is preserved.
/// </para>
/// </summary>
public sealed record AdminResetPasswordCommand(
    Guid TargetUserId,
    string? Reason) : ICommand<AdminResetPasswordResult>;
