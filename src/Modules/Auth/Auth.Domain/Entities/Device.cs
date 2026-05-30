using YallaJo.SharedKernel.Domain.Entities;

namespace Auth.Domain.Entities;


public sealed class Device : AuditableEntity, IAggregateRoot
{
    private Device() { } // EF Core

    public Guid UserId { get; private set; }
    public string DeviceToken { get; private set; } = string.Empty;
    public string? UserAgent { get; private set; }
    public string? DeviceName { get; private set; }
    public bool IsTrusted { get; private set; }
    public DateTime? TrustedAt { get; private set; }
    public DateTime LastSeenAt { get; private set; }

    public static Device Create(Guid userId, string deviceToken, string? userAgent = null, string? deviceName = null)
    {
        return new Device
        {
            UserId = userId,
            DeviceToken = deviceToken,
            UserAgent = userAgent,
            DeviceName = deviceName,
            IsTrusted = false,
            LastSeenAt = DateTime.UtcNow
        };
    }

    public void Trust()
    {
        IsTrusted = true;
        TrustedAt = DateTime.UtcNow;
        MarkUpdated();
    }

    public void RecordSeen()
    {
        LastSeenAt = DateTime.UtcNow;
        MarkUpdated();
    }
}
