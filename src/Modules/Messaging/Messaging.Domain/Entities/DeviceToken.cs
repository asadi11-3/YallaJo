using Messaging.Domain.Enums;
using Messaging.Domain.Events;
using YallaJo.SharedKernel.Domain.Entities;

namespace Messaging.Domain.Entities;

public sealed class DeviceToken : AuditableEntity, IAggregateRoot
{
    private DeviceToken() { } // EF Core

    /// <summary>Owner user ID.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Stable hardware/app device identifier (max 200 chars). Used for UPSERT deduplication.</summary>
    public string DeviceId { get; private set; } = string.Empty;

    /// <summary>FCM / APNs / Web push token (max 500 chars). Never exposed in GET DTOs.</summary>
    public string Token { get; private set; } = string.Empty;

    /// <summary>Target push platform.</summary>
    public DevicePlatform Platform { get; private set; }

    /// <summary>UTC timestamp of last push delivery (used to detect stale tokens >30 days).</summary>
    public DateTime LastSeenAt { get; private set; }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static DeviceToken Register(
        Guid userId,
        string deviceId,
        string token,
        DevicePlatform platform,
        TimeProvider timeProvider)
    {
        if (userId == Guid.Empty) throw new ArgumentException("UserId required.", nameof(userId));
        if (string.IsNullOrWhiteSpace(deviceId)) throw new ArgumentException("DeviceId required.", nameof(deviceId));
        if (string.IsNullOrWhiteSpace(token)) throw new ArgumentException("Token required.", nameof(token));

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var dt = new DeviceToken
        {
            UserId     = userId,
            DeviceId   = deviceId.Trim(),
            Token      = token.Trim(),
            Platform   = platform,
            LastSeenAt = now,
        };
        dt.AddDomainEvent(new DeviceTokenRegisteredDomainEvent(dt.Id, userId, platform));
        return dt;
    }

    // ── Business Methods ──────────────────────────────────────────────────────

    /// <summary>Updates the push token (e.g. FCM token rotation).</summary>
    public void UpdateToken(string newToken)
    {
        if (string.IsNullOrWhiteSpace(newToken)) throw new ArgumentException("Token required.", nameof(newToken));
        Token = newToken.Trim();
        MarkUpdated();
    }

    /// <summary>Records a successful push delivery, refreshing staleness window.</summary>
    public void MarkSeen(DateTime nowUtc)
    {
        LastSeenAt = nowUtc;
        MarkUpdated();
    }

    /// <summary>Soft-deletes the token (idempotent).</summary>
    public void Delete()
    {
        if (IsDeleted) return;
        SoftDelete();
        AddDomainEvent(new DeviceTokenRevokedDomainEvent(Id, UserId));
    }
}
