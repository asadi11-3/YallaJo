using Messaging.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;

namespace Messaging.Domain.Entities;

public sealed class Notification : BaseEntity
{
    private Notification() { } // EF Core

    public Guid UserId { get; private set; }
    public NotificationType Type { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public NotificationPriority Priority { get; private set; } = NotificationPriority.Low;
    public string Title { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public string? Data { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime? ReadAt { get; private set; }
    public DateTime? SentAt { get; private set; }
    public string? EntityType { get; private set; }
    public Guid? EntityId { get; private set; }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static Notification Create(
        Guid userId,
        NotificationType type,
        NotificationChannel channel,
        string title,
        string body,
        NotificationPriority priority = NotificationPriority.Medium,
        string? data = null,
        string? entityType = null,
        Guid? entityId = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(body))
            throw new ArgumentException("Body is required.", nameof(body));

        return new Notification
        {
            Id         = Guid.CreateVersion7(),
            UserId     = userId,
            Type       = type,
            Channel    = channel,
            Priority   = priority,
            Title      = title.Trim(),
            Body       = body.Trim(),
            Data       = data,
            IsRead     = false,
            EntityType = entityType,
            EntityId   = entityId,
        };
    }

    // ── Business Methods ──────────────────────────────────────────────────────

    public void MarkSent()
    {
        SentAt = DateTime.UtcNow;
    }

    public void MarkRead()
    {
        IsRead = true;
        ReadAt = DateTime.UtcNow;
    }
}
