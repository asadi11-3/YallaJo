using YallaJo.SharedKernel.Domain.Entities;

namespace Auth.Domain.Entities;

public sealed class Otp : AuditableEntity
{
    private const int MaxAttempts = 5;

    private Otp() { } // EF Core

    public Guid UserId { get; private set; }
    public string Purpose { get; private set; } = string.Empty;
    public string CodeHash { get; private set; } = string.Empty;
    public string DeliveryChannel { get; private set; } = string.Empty;
    public string DeliveryAddress { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public int AttemptCount { get; private set; }
    public bool IsUsed { get; private set; }
    public DateTime? UsedAt { get; private set; }

    // ── Domain invariants ─────────────────────────────────────────────────────
    /// <summary>Returns true when the OTP's time window has passed.</summary>
    public bool IsExpired() => DateTime.UtcNow > ExpiresAt;

    /// <summary>
    /// Returns true when the maximum number of verification attempts has been reached.
    /// Handlers must check this BEFORE incrementing to avoid one extra attempt leak.
    /// </summary>
    public bool IsExhausted => AttemptCount >= MaxAttempts;

    public static Otp Create(
        Guid userId,
        string purpose,
        string codeHash,
        string deliveryChannel,
        string deliveryAddress,
        int expiryMinutes = 10)
    {
        return new Otp
        {
            UserId          = userId,
            Purpose         = purpose,
            CodeHash        = codeHash,
            DeliveryChannel = deliveryChannel,
            DeliveryAddress = deliveryAddress,
            ExpiresAt       = DateTime.UtcNow.AddMinutes(expiryMinutes),
            AttemptCount    = 0,
            IsUsed          = false
        };
    }

    public void IncrementAttempt() => AttemptCount++;

    public void MarkUsed()
    {
        IsUsed  = true;
        UsedAt  = DateTime.UtcNow;
        MarkUpdated();
    }
}
