using YallaJo.SharedKernel.Domain.Entities;

namespace Auth.Domain.Entities;

/// <summary>
/// An authenticated session tied to a user and device.
/// UserId references Security.User.Id (no FK, cross-DB).
/// </summary>
public sealed class Session : AuditableEntity, IAggregateRoot
{
    private Session() { } // EF Core

    public Guid UserId { get; private set; }
    public Guid DeviceId { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? IpAddress { get; private set; }

    public static Session Create(Guid userId, Guid deviceId, DateTime expiresAt, string? ipAddress = null)
    {
        return new Session
        {
            UserId = userId,
            DeviceId = deviceId,
            ExpiresAt = expiresAt,
            IsRevoked = false,
            IpAddress = ipAddress
        };
    }

    public void Revoke()
    {
        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
        MarkUpdated();
    }
}
