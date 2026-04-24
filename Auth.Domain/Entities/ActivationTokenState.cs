namespace Auth.Domain.Entities;

/// <summary>
/// Explicit lifecycle of an <see cref="ActivationToken"/>. Phase 2C-1
/// replaces the overloaded <c>Otp(Purpose="UserInvite")</c> row with a
/// dedicated aggregate, so activation tokens get a state machine that
/// distinguishes "sent", "consumed", and "revoked" cleanly instead of
/// smearing them across a single <c>IsUsed</c> boolean.
/// <para>
/// Allowed transitions (enforced by <c>ActivationToken</c> verbs):
/// <code>
///   Issued     → Delivered | Revoked
///   Delivered  → Consumed  | Revoked
///   Consumed   → ∅ (terminal)
///   Revoked    → ∅ (terminal)
///   Expired    → ∅ (virtual — inferred from ExpiresAt at validation time;
///                    no explicit transition exists in 2C-1 to avoid
///                    requiring a background sweeper)
/// </code>
/// </para>
/// </summary>
public enum ActivationTokenState
{
    /// <summary>
    /// Persisted but the activation email has not yet been dispatched.
    /// Momentary state — the <c>SendActivationEmail</c> handler transitions
    /// to <see cref="Delivered"/> or <see cref="Revoked"/> in the same call
    /// depending on SMTP outcome.
    /// </summary>
    Issued = 0,

    /// <summary>
    /// Activation email was successfully handed to the SMTP relay. The
    /// token is redeemable until it expires, is consumed, or is revoked.
    /// </summary>
    Delivered = 1,

    /// <summary>
    /// Token was redeemed by the user completing activation. Terminal.
    /// </summary>
    Consumed = 2,

    /// <summary>
    /// Token was invalidated before the user could consume it — superseded
    /// by a resend, revoked by an admin, or revoked automatically because
    /// email delivery failed. Terminal. See
    /// <see cref="ActivationTokenRevokedReason"/> for the why.
    /// </summary>
    Revoked = 3,

    /// <summary>
    /// Reserved: explicit expired state for future sweeper jobs. Not used
    /// by Phase 2C-1 code; expiry is evaluated from <c>ExpiresAt</c> at
    /// validation time and surfaces as a distinct error without writing
    /// the state column.
    /// </summary>
    Expired = 4,
}
