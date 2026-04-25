using YallaJo.SharedKernel.Domain.Entities;

namespace Auth.Domain.Entities;

/// <summary>
/// Phase 2C-2 — dedicated aggregate root for password-reset tokens.
/// Replaces the overloaded <c>Otp(Purpose="PasswordReset")</c>
/// representation with an explicit state machine, separate lifecycle +
/// delivery-status fields, a typed revocation reason, and an origin
/// column that records which flow issued the token (self-service vs
/// future admin / reassignment).
/// <para>
/// Only the token's HASH is stored — the plain code is returned to the
/// caller once at creation time (to embed in the reset email) and is
/// re-derived from the user-presented code at validation time via the
/// existing <c>IOtpService</c> hashing primitive (carried forward from
/// the Otp era so 2C-2 does not invent a second protocol).
/// </para>
/// <para>
/// Deliberately parallel in shape to <see cref="ActivationToken"/> so
/// future generalization (base token aggregate, shared audit log
/// subscriber) is a mechanical refactor rather than a redesign.
/// </para>
/// <para>
/// One non-terminal token per user is enforced in code by the
/// <c>ForgotPasswordCommand</c> handler, which revokes any existing
/// <see cref="PasswordResetTokenState.Issued"/> or
/// <see cref="PasswordResetTokenState.Delivered"/> rows before
/// persisting a new one. A filtered-unique DB index is deliberately NOT
/// introduced in 2C-2 (same rationale as ActivationToken: the
/// predicate is awkward, the code path is serialized per user, and
/// concurrent resets are rare — revisit if observed).
/// </para>
/// </summary>
public sealed class PasswordResetToken : AuditableEntity, IAggregateRoot
{
    private const int MaxAttempts = 5;

    private PasswordResetToken() { } // EF Core

    public Guid UserId { get; private set; }

    /// <summary>
    /// Cryptographic hash of the plain reset code/token. The plain code
    /// is returned to the caller once (to embed in the reset email) and
    /// is never persisted.
    /// </summary>
    public string TokenHash { get; private set; } = string.Empty;

    /// <summary>
    /// Email the reset code was sent to. Kept on the token — separate
    /// from the user's primary email column — so future admin flows
    /// (reassignment, alternate email) can retarget a reset without
    /// mutating the user aggregate.
    /// </summary>
    public string DeliveryAddress { get; private set; } = string.Empty;

    public DateTime IssuedAt  { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    public DateTime? ConsumedAt { get; private set; }
    public DateTime? RevokedAt  { get; private set; }
    public PasswordResetTokenRevokedReason RevokedReason { get; private set; } = PasswordResetTokenRevokedReason.None;

    public PasswordResetTokenState State { get; private set; } = PasswordResetTokenState.Issued;
    public PasswordResetTokenDeliveryStatus DeliveryStatus { get; private set; } = PasswordResetTokenDeliveryStatus.Pending;
    public DateTime? LastSentAt { get; private set; }

    /// <summary>
    /// Which flow issued this token. Phase 2C-2 only permits
    /// <see cref="PasswordResetOrigin.SelfService"/>; the other values
    /// are reserved for later admin flows.
    /// </summary>
    public PasswordResetOrigin ResetOrigin { get; private set; } = PasswordResetOrigin.SelfService;

    /// <summary>
    /// Bounded attempt counter so a brute-force attacker cannot grind
    /// through the code hash space by flooding the reset endpoint.
    /// Matches the legacy <c>Otp.MaxAttempts</c> (5) deliberately so
    /// 2C-2 does not change user-visible rate-limit behaviour.
    /// </summary>
    public int AttemptCount { get; private set; }

    public bool IsExpired(DateTime? nowUtc = null)
        => (nowUtc ?? DateTime.UtcNow) > ExpiresAt;

    public bool IsExhausted => AttemptCount >= MaxAttempts;

    /// <summary>
    /// True when the token's lifecycle state is terminal (Consumed or
    /// Revoked). Handlers use this to short-circuit validation against
    /// a row that is no longer redeemable.
    /// </summary>
    public bool IsTerminal => State == PasswordResetTokenState.Consumed
                           || State == PasswordResetTokenState.Revoked;

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new reset token in <see cref="PasswordResetTokenState.Issued"/>
    /// / <see cref="PasswordResetTokenDeliveryStatus.Pending"/>. The
    /// caller is expected to invoke <see cref="MarkDelivered"/>
    /// immediately after a successful email dispatch or
    /// <see cref="RevokeOnEmailFailure"/> on SMTP failure, in the same
    /// unit of work.
    /// </summary>
    public static PasswordResetToken Issue(
        Guid userId,
        string tokenHash,
        string deliveryAddress,
        int expiryMinutes,
        PasswordResetOrigin origin = PasswordResetOrigin.SelfService,
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

        return new PasswordResetToken
        {
            UserId          = userId,
            TokenHash       = tokenHash,
            DeliveryAddress = deliveryAddress,
            IssuedAt        = now,
            ExpiresAt       = now.AddMinutes(expiryMinutes),
            State           = PasswordResetTokenState.Issued,
            DeliveryStatus  = PasswordResetTokenDeliveryStatus.Pending,
            RevokedReason   = PasswordResetTokenRevokedReason.None,
            ResetOrigin     = origin,
            AttemptCount    = 0,
        };
    }

    // ── State transitions ─────────────────────────────────────────────────────

    /// <summary>
    /// Email was successfully handed to the SMTP relay.
    /// <c>Issued → Delivered</c>. Idempotent on <c>Delivered</c>.
    /// </summary>
    public void MarkDelivered(DateTime? nowUtc = null)
    {
        if (State != PasswordResetTokenState.Issued && State != PasswordResetTokenState.Delivered)
            throw new InvalidPasswordResetTokenTransitionException(State, PasswordResetTokenState.Delivered);

        State          = PasswordResetTokenState.Delivered;
        DeliveryStatus = PasswordResetTokenDeliveryStatus.Sent;
        LastSentAt     = nowUtc ?? DateTime.UtcNow;
        MarkUpdated();
    }

    /// <summary>
    /// User successfully redeemed the token during reset.
    /// <c>Delivered → Consumed</c>. Also accepted from <c>Issued</c> for
    /// the same race-safety rationale documented on
    /// <see cref="ActivationToken.Consume"/>.
    /// </summary>
    public void Consume(DateTime? nowUtc = null)
    {
        if (State != PasswordResetTokenState.Delivered && State != PasswordResetTokenState.Issued)
            throw new InvalidPasswordResetTokenTransitionException(State, PasswordResetTokenState.Consumed);

        State      = PasswordResetTokenState.Consumed;
        ConsumedAt = nowUtc ?? DateTime.UtcNow;
        MarkUpdated();
    }

    /// <summary>
    /// A newer reset token was issued for the same user. Transitions
    /// <c>Issued | Delivered → Revoked</c> with
    /// <see cref="PasswordResetTokenRevokedReason.Superseded"/>.
    /// Terminal states are silently ignored so the supersede sweep is
    /// idempotent.
    /// </summary>
    public void Supersede(DateTime? nowUtc = null)
        => Revoke(PasswordResetTokenRevokedReason.Superseded, nowUtc);

    /// <summary>
    /// Admin explicitly revoked the outstanding token (reserved —
    /// present so the state machine is complete).
    /// </summary>
    public void RevokeByAdmin(DateTime? nowUtc = null)
        => Revoke(PasswordResetTokenRevokedReason.AdminRevoked, nowUtc);

    /// <summary>
    /// Email delivery failed — auto-revoke so the row is not usable and
    /// the operator can audit which sends have failed. Updates
    /// <see cref="DeliveryStatus"/> to
    /// <see cref="PasswordResetTokenDeliveryStatus.Failed"/> on top of
    /// the standard revoke bookkeeping.
    /// <para>
    /// Phase 2C-3: retained for TERMINAL / permanent revocation only
    /// (admin cancels a stuck token, outbox exhausts retries and ops
    /// intervenes). The event-driven email dispatcher uses
    /// <see cref="MarkDeliveryFailed"/> instead so transient SMTP
    /// failures remain retryable.
    /// </para>
    /// </summary>
    public void RevokeOnEmailFailure(DateTime? nowUtc = null)
    {
        Revoke(PasswordResetTokenRevokedReason.EmailFailed, nowUtc);
        DeliveryStatus = PasswordResetTokenDeliveryStatus.Failed;
    }

    /// <summary>
    /// Phase 2C-3 — non-terminal delivery-failure marker used by the
    /// event-driven email dispatcher when SMTP throws a transient error.
    /// Keeps the token redeemable (<see cref="State"/> stays
    /// <see cref="PasswordResetTokenState.Issued"/> or
    /// <see cref="PasswordResetTokenState.Delivered"/>) so the outbox
    /// can retry with the same valid token. Flips
    /// <see cref="DeliveryStatus"/> to
    /// <see cref="PasswordResetTokenDeliveryStatus.Failed"/> and stamps
    /// <see cref="LastSentAt"/> so ops can see when the last attempt
    /// happened.
    /// <para>
    /// Legal from <see cref="PasswordResetTokenState.Issued"/> or
    /// <see cref="PasswordResetTokenState.Delivered"/>. No-op on
    /// terminal states so a retry that arrives after the user already
    /// consumed the token doesn't corrupt the audit trail.
    /// </para>
    /// </summary>
    public void MarkDeliveryFailed(DateTime? nowUtc = null)
    {
        if (IsTerminal)
            return; // no-op on Consumed / Revoked — preserves audit trail

        if (State != PasswordResetTokenState.Issued && State != PasswordResetTokenState.Delivered)
            throw new InvalidPasswordResetTokenTransitionException(State, State); // defensive

        DeliveryStatus = PasswordResetTokenDeliveryStatus.Failed;
        LastSentAt     = nowUtc ?? DateTime.UtcNow;
        MarkUpdated();
    }

    private void Revoke(PasswordResetTokenRevokedReason reason, DateTime? nowUtc)
    {
        if (IsTerminal)
            return; // idempotent — no-op for already-terminal tokens

        if (State != PasswordResetTokenState.Issued && State != PasswordResetTokenState.Delivered)
            throw new InvalidPasswordResetTokenTransitionException(State, PasswordResetTokenState.Revoked);

        State         = PasswordResetTokenState.Revoked;
        RevokedAt     = nowUtc ?? DateTime.UtcNow;
        RevokedReason = reason;
        MarkUpdated();
    }

    /// <summary>
    /// Increment the verification attempt counter. Handlers must call
    /// this BEFORE comparing hashes so the counter reflects every
    /// check, and must short-circuit on <see cref="IsExhausted"/>
    /// BEFORE the increment to avoid one-extra-attempt drift.
    /// </summary>
    public void IncrementAttempt()
    {
        AttemptCount++;
        MarkUpdated();
    }
}
