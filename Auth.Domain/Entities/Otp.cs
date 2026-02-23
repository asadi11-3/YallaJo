using YallaJo.SharedKernel.Domain.Entities;

namespace Auth.Domain.Entities;

/// <summary>
/// One-time password for verification flows (email, phone, 2FA).
/// UserId references Security.User.Id (no FK, cross-DB).
/// CodeHash is stored — never the raw code.
/// </summary>
public sealed class Otp : AuditableEntity, IAggregateRoot
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
        Guid userId, string purpose, string codeHash,
        string deliveryChannel, string deliveryAddress, DateTime expiresAt)
    {
        return new Otp
        {
            UserId = userId,
            Purpose = purpose,
            CodeHash = codeHash,
            DeliveryChannel = deliveryChannel,
            DeliveryAddress = deliveryAddress,
            ExpiresAt = expiresAt,
            AttemptCount = 0,
            IsUsed = false
        };
    }

    public void RecordAttempt()
    {
        AttemptCount++;
        MarkUpdated();
    }

    public void MarkUsed()
    {
        IsUsed = true;
        UsedAt = DateTime.UtcNow;
        MarkUpdated();
    }
}
