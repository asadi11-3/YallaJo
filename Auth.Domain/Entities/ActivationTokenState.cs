namespace Auth.Domain.Entities;

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
