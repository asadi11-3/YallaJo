using YallaJo.SharedKernel.Domain.Entities;

namespace Auth.Domain.Entities;


public sealed class Otp : AuditableEntity
{
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
            UserId = userId,
            Purpose = purpose,
            CodeHash = codeHash,
            DeliveryChannel = deliveryChannel,
            DeliveryAddress = deliveryAddress,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes),
            AttemptCount = 0,
            IsUsed = false
        };
    }

    public bool IsExpired() => DateTime.UtcNow > ExpiresAt;

    public void IncrementAttempt() => AttemptCount++;

    public void MarkUsed()
    {
        IsUsed = true;
        UsedAt = DateTime.UtcNow;
        MarkUpdated();
    }
}
