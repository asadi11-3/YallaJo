using YallaJo.SharedKernel.Domain.Entities;

namespace Auth.Domain.Entities;

public sealed class ActivationToken : AuditableEntity, IAggregateRoot
{
    private const int MaxAttempts = 5;

    private ActivationToken() { } // EF Core

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public string DeliveryAddress { get; private set; } = string.Empty;

    public DateTime IssuedAt  { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    public DateTime? ConsumedAt    { get; private set; }
    public DateTime? RevokedAt     { get; private set; }
    public ActivationTokenRevokedReason RevokedReason { get; private set; } = ActivationTokenRevokedReason.None;

    public ActivationTokenState State { get; private set; } = ActivationTokenState.Issued;
    public ActivationTokenDeliveryStatus DeliveryStatus { get; private set; } = ActivationTokenDeliveryStatus.Pending;
    public DateTime? LastSentAt { get; private set; }

    public int AttemptCount { get; private set; }

    public bool IsExpired(DateTime? nowUtc = null)
        => (nowUtc ?? DateTime.UtcNow) > ExpiresAt;

    public bool IsExhausted => AttemptCount >= MaxAttempts;

    public bool IsTerminal => State == ActivationTokenState.Consumed
                           || State == ActivationTokenState.Revoked;

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

    public void MarkDelivered(DateTime? nowUtc = null)
    {
        if (State != ActivationTokenState.Issued && State != ActivationTokenState.Delivered)
            throw new InvalidActivationTokenTransitionException(State, ActivationTokenState.Delivered);

        State          = ActivationTokenState.Delivered;
        DeliveryStatus = ActivationTokenDeliveryStatus.Sent;
        LastSentAt     = nowUtc ?? DateTime.UtcNow;
        MarkUpdated();
    }

    public void Consume(DateTime? nowUtc = null)
    {
        if (State != ActivationTokenState.Delivered && State != ActivationTokenState.Issued)
            throw new InvalidActivationTokenTransitionException(State, ActivationTokenState.Consumed);

        State      = ActivationTokenState.Consumed;
        ConsumedAt = nowUtc ?? DateTime.UtcNow;
        MarkUpdated();
    }

    public void Supersede(DateTime? nowUtc = null)
        => Revoke(ActivationTokenRevokedReason.Superseded, nowUtc);

    public void RevokeByAdmin(DateTime? nowUtc = null)
        => Revoke(ActivationTokenRevokedReason.AdminRevoked, nowUtc);

    public void RevokeOnEmailFailure(DateTime? nowUtc = null)
    {
        Revoke(ActivationTokenRevokedReason.EmailFailed, nowUtc);
        DeliveryStatus = ActivationTokenDeliveryStatus.Failed;
    }

    public void MarkDeliveryFailed(DateTime? nowUtc = null)
    {
        if (IsTerminal)
            return; // no-op on Consumed / Revoked — preserves audit trail

        if (State != ActivationTokenState.Issued && State != ActivationTokenState.Delivered)
            throw new InvalidActivationTokenTransitionException(State, State); // defensive

        DeliveryStatus = ActivationTokenDeliveryStatus.Failed;
        LastSentAt     = nowUtc ?? DateTime.UtcNow;
        MarkUpdated();
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

    public void IncrementAttempt()
    {
        AttemptCount++;
        MarkUpdated();
    }
}
