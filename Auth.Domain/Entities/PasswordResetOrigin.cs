namespace Auth.Domain.Entities;

/// <summary>
/// Captures who initiated the password-reset flow that created a
/// <see cref="PasswordResetToken"/>. Persisted on the aggregate so
/// downstream telemetry / audit can distinguish a self-service reset
/// (user clicked Forgot Password) from admin-driven flows introduced in
/// later phases.
/// <para>
/// Phase 2C-2 only wires <see cref="SelfService"/>. The other values
/// exist so the enum shape does not churn when the remaining flows are
/// added; callers other than <see cref="SelfService"/> are not permitted
/// on the <c>ForgotPasswordCommand</c> path.
/// </para>
/// </summary>
public enum PasswordResetOrigin
{
    /// <summary>
    /// User-initiated self-service recovery — clicked Forgot Password.
    /// The only origin wired in Phase 2C-2.
    /// </summary>
    SelfService = 0,

    /// <summary>
    /// Admin-initiated reset (Phase 2C+). Reserved — the aggregate
    /// accepts this origin but no handler issues it yet.
    /// </summary>
    AdminInitiated = 1,

    /// <summary>
    /// Account reassignment flow (Phase 2C+). Reserved — the aggregate
    /// accepts this origin but no handler issues it yet.
    /// </summary>
    Reassignment = 2,
}
