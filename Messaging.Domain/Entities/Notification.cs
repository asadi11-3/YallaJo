using Messaging.Domain.Enums;
using Messaging.Domain.Events;
using YallaJo.SharedKernel.Domain.Entities;
using YallaJo.SharedKernel.Domain.Event;

namespace Messaging.Domain.Entities;

public sealed class Notification : AuditableEntity, IAggregateRoot
{
    private Notification() { } // EF Core

    public Guid UserId { get; private set; }
    public NotificationType Type { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public NotificationPriority Priority { get; private set; } = NotificationPriority.Medium;
    public string Title { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public string? Data { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime? ReadAt { get; private set; }
    public DateTime? SentAt { get; private set; }
    public string? ExternalRef { get; private set; }
    public string? FailureReason { get; private set; }
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

        var notification = new Notification
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

        notification.AddDomainEvent(new NotificationCreatedDomainEvent(
            notification.Id, userId, type, channel, priority));

        return notification;
    }

    // ── Business Methods ──────────────────────────────────────────────────────

    /// <summary>Marks notification as sent by the delivery service. Idempotent.</summary>
    public void MarkSent(string? externalRef = null)
    {
        if (SentAt.HasValue) return; // idempotent
        SentAt = DateTime.UtcNow;
        ExternalRef = externalRef;
        AddDomainEvent(new NotificationDeliveredDomainEvent(Id, UserId, Channel, SentAt.Value));
        MarkUpdated();
    }

    /// <summary>Marks notification as read. Idempotent.</summary>
    public void MarkRead()
    {
        if (IsRead) return; // idempotent
        IsRead = true;
        ReadAt = DateTime.UtcNow;
        AddDomainEvent(new NotificationReadDomainEvent(Id, UserId, ReadAt.Value));
        MarkUpdated();
    }

    /// <summary>Marks notification delivery as permanently failed.</summary>
    public void MarkFailed(string reason)
    {
        FailureReason = reason;
        AddDomainEvent(new NotificationFailedDomainEvent(Id, UserId, Channel, reason));
        MarkUpdated();
    }
}
