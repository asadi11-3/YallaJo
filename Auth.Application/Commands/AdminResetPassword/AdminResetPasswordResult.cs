namespace Auth.Application.Commands.AdminResetPassword;

/// <summary>
/// Phase 3A — result payload for <see cref="AdminResetPasswordCommand"/>.
/// Carries only an admin-facing confirmation message; the plain reset
/// code is NEVER included here. The code reaches the user out-of-band
/// via the <c>PasswordResetEmailDispatchHandler</c>.
/// </summary>
public sealed record AdminResetPasswordResult(string Message);
