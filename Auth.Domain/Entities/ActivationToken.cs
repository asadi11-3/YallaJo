using YallaJo.SharedKernel.Domain.Entities;

namespace Auth.Domain.Entities;

/// <summary>
/// Phase 2C-1 — dedicated aggregate root for activation tokens. Replaces
/// the overloaded <c>Otp(Purpose="UserInvite")</c> representation with an
/// explicit state machine, separate lifecycle + delivery-status fields,
/// and a typed revocation reason. Only the token's HASH is stored — the
/// plain token is returned to the caller once at creation time and is
/// re-derived from the user-presented token at validation time via the
/// <c>IInviteTokenService</c> (shared Phase 2B hashing primitive, kept
/// deliberately to avoid inventing a second hashing protocol this phase).
/// <para>
/// One non-terminal token per user is enforced in code by the
/// <c>SendActivationEmailCommand</c> handler, which revokes any existing
/// <see cref="ActivationTokenState.Issued"/> or
/// <see cref="ActivationTokenState.Delivered"/> rows before persisting a
/// new one. A filtered-unique DB index is deliberately NOT introduced in
/// 2C-1 — the state machine makes the predicate awkward (Issued OR
/// Delivered, not IsUsed) and the code path is serialized by the
/// per-user admin workflow anyway. Revisit in 2C-2 if concurrent
/// supersede races become observable.
/// </para>
/// </summary>
public sealed class ActivationToken : AuditableEntity, IAggregateRoot
{
    private const int MaxAttempts = 5;

    private ActivationToken() { } // EF Core

    public Guid UserId { get; private set; }

    /// <summary>
    /// Cryptographic hash of the plain activation token. The plain token
    /// is returned to the caller once (to embed in the activation URL)
    /// and is never persisted.
    /// </summary>
    public string TokenHash { get; private set; } = string.Empty;

    /// <summary>
    /// Email (or future: phone / other channel address) the activation
    /// link was sent to. Kept on the token — separate from the user's
    /// primary email column — so admin reassignment scenarios in a later
    /// phase can retarget the next activation link without mutating the
    /// user aggregate.
    /// </summary>
    public string DeliveryAddress { get; private set; } = string.Empty;

    public DateTime IssuedAt  { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    public DateTime? ConsumedAt    { get; private set; }
    public DateTime? RevokedAt     { get; private set; }
    public ActivationTokenRevokedReason RevokedReason { get; private set; } = ActivationTokenRevokedReason.None;

    public ActivationTokenState State { get; private set; } = ActivationTokenState.Issued;
    public ActivationTokenDeliveryStatus DeliveryStatus { get; private set; } = ActivationTokenDeliveryStatus.Pending;
    public DateTime? LastSentAt { get; private set; }

    /// <summary>
    /// Bounded attempt counter so a brute-force attacker cannot grind
    /// through the token hash space by flooding the activation endpoint.
    /// Matches the legacy <c>Otp.MaxAttempts</c> (5) deliberately so 2C-1
    /// does not change user-visible rate-limit behaviour.
    /// </summary>
    public int AttemptCount { get; private set; }

    public bool IsExpired(DateTime? nowUtc = null)
        => (nowUtc ?? DateTime.UtcNow) > ExpiresAt;

    public bool IsExhausted => AttemptCount >= MaxAttempts;

    /// <summary>
    /// True when the token's lifecycle state is terminal (Consumed or
    /// Revoked). Handlers use this to short-circuit validation against a
    /// row that is no longer redeemable.
    /// </summary>
    public bool IsTerminal => State == ActivationTokenState.Consumed
                           || State == ActivationTokenState.Revoked;

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new activation token in <see cref="ActivationTokenState.Issued"/>
    /// / <see cref="ActivationTokenDeliveryStatus.Pending"/>. The caller is
    /// expected to invoke <see cref="MarkDelivered"/> immediately after a
    /// successful email dispatch or <see cref="RevokeOnEmailFailure"/>
    /// on SMTP failure, in the same unit of work.
    /// </summary>
    public static ActivationToken Issue(
        Guid userId,
        string tokenHash,
        string deliveryAddress,
        int expiryMinutes,
        DateTime? nowUtc = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));
        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new ArgumentException("Token hash is required.", nameof(tokenHash));
        if (string.IsNullOrWhiteSpace(deliveryAddress))
            throw new ArgumentException("Delivery address is required.", nameof(deliveryAddress));
        if (expiryMinutes <= 0)
            throw new ArgumentOutOfRangeException(nameof(expiryMinutes), "Must be positive.");

        var now = nowUtc ?? DateTime.UtcNow;

        return new ActivationToken
        {
            UserId          = userId,
            TokenHash       = tokenHash,
            DeliveryAddress = deliveryAddress,
            IssuedAt        = now,
            ExpiresAt       = now.AddMinutes(expiryMinutes),
            State           = ActivationTokenState.Issued,
            DeliveryStatus  = ActivationTokenDeliveryStatus.Pending,
            RevokedReason   = ActivationTokenRevokedReason.None,
            AttemptCount    = 0,
        };
    }

    // ── State transitions ─────────────────────────────────────────────────────

    /// <summary>
    /// Email was successfully handed to the SMTP relay.
    /// <c>Issued → Delivered</c>. Idempotent on <c>Delivered</c> (a resend
    /// that re-dispatches the same row just refreshes <see cref="LastSentAt"/>).
    /// </summary>
    public void MarkDelivered(DateTime? nowUtc = null)
    {
        if (State != ActivationTokenState.Issued && State != ActivationTokenState.Delivered)
            throw new InvalidActivationTokenTransitionException(State, ActivationTokenState.Delivered);

        State          = ActivationTokenState.Delivered;
        DeliveryStatus = ActivationTokenDeliveryStatus.Sent;
        LastSentAt     = nowUtc ?? DateTime.UtcNow;
        MarkUpdated();
    }

    /// <summary>
    /// User successfully redeemed the token during activation.
    /// <c>Delivered → Consumed</c>. Also accepted from <c>Issued</c> for
    /// safety — an activation attempt could, in principle, race ahead of
    /// the delivery mark if the email provider is unusually fast; that is
    /// still a legitimate consumption.
    /// </summary>
    public void Consume(DateTime? nowUtc = null)
    {
        if (State != ActivationTokenState.Delivered && State != ActivationTokenState.Issued)
            throw new InvalidActivationTokenTransitionException(State, ActivationTokenState.Consumed);

        State      = ActivationTokenState.Consumed;
        ConsumedAt = nowUtc ?? DateTime.UtcNow;
        MarkUpdated();
    }

    /// <summary>
    /// A newer activation send was issued for the same user. Transitions
    /// <c>Issued | Delivered → Revoked</c> with
    /// <see cref="ActivationTokenRevokedReason.Superseded"/>. Terminal
    /// states are silently ignored so the supersede-sweep is idempotent.
    /// </summary>
    public void Supersede(DateTime? nowUtc = null)
        => Revoke(ActivationTokenRevokedReason.Superseded, nowUtc);

    /// <summary>
    /// Admin explicitly revoked the outstanding token (Phase 2C+ admin UI).
    /// </summary>
    public void RevokeByAdmin(DateTime? nowUtc = null)
        => Revoke(ActivationTokenRevokedReason.AdminRevoked, nowUtc);

    /// <summary>
    /// Email delivery failed — auto-revoke so the row is not usable and
    /// the operator can audit which sends have failed. Updates
    /// <see cref="DeliveryStatus"/> to <see cref="ActivationTokenDeliveryStatus.Failed"/>
    /// on top of the standard revoke bookkeeping.
    /// </summary>
    public void RevokeOnEmailFailure(DateTime? nowUtc = null)
    {
        Revoke(ActivationTokenRevokedReason.EmailFailed, nowUtc);
        DeliveryStatus = ActivationTokenDeliveryStatus.Failed;
    }

    private void Revoke(ActivationTokenRevokedReason reason, DateTime? nowUtc)
    {
        if (IsTerminal)
            return; // idempotent — no-op for already-terminal tokens

        if (State != ActivationTokenState.Issued && State != ActivationTokenState.Delivered)
            throw new InvalidActivationTokenTransitionException(State, ActivationTokenState.Revoked);

        State         = ActivationTokenState.Revoked;
        RevokedAt     = nowUtc ?? DateTime.UtcNow;
        RevokedReason = reason;
        MarkUpdated();
    }

    /// <summary>
    /// Increment the verification attempt counter. Handlers must call
    /// this BEFORE comparing hashes so the counter reflects every check,
    /// and must short-circuit on <see cref="IsExhausted"/> BEFORE the
    /// increment to avoid one-extra-attempt drift.
    /// </summary>
    public void IncrementAttempt()
    {
        AttemptCount++;
        MarkUpdated();
    }
}
