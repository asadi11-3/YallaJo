using Messaging.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;

namespace Messaging.Domain.Entities;

/// <summary>
/// Tracks individual delivery attempts for Email / Push notifications.
/// Status drives the retry loop in EmailNotificationSenderService (T5).
/// </summary>
public sealed class NotificationDeliveryAttempt : BaseEntity
{
    private NotificationDeliveryAttempt() { } // EF Core

    /// <summary>Parent notification.</summary>
    public Guid NotificationId { get; private set; }

    /// <summary>Delivery channel (Email, Push, ...).</summary>
    public NotificationChannel Channel { get; private set; }

    /// <summary>UTC timestamp when the attempt record was created (due date for first attempt).</summary>
    public DateTime AttemptedAt { get; private set; }

    /// <summary>UTC timestamp when this attempt completed (success or failure).</summary>
    public DateTime? CompletedAt { get; private set; }

    /// <summary>Current delivery status.</summary>
    public NotificationDeliveryStatus Status { get; private set; } = NotificationDeliveryStatus.Pending;

    /// <summary>1-based attempt counter.</summary>
    public int AttemptNumber { get; private set; }

    /// <summary>Failure description when Status=Failed.</summary>
    public string? FailureReason { get; private set; }

    /// <summary>Provider-side message/receipt ID on success.</summary>
    public string? ExternalRef { get; private set; }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static NotificationDeliveryAttempt Create(
        Guid notificationId,
        NotificationChannel channel,
        int attemptNumber = 1)
    {
        return new NotificationDeliveryAttempt
        {
            NotificationId = notificationId,
            Channel        = channel,
            AttemptNumber  = attemptNumber,
            AttemptedAt    = DateTime.UtcNow,
            Status         = NotificationDeliveryStatus.Pending,
        };
    }

    // ── Business Methods ──────────────────────────────────────────────────────

    public void MarkSucceeded(string? externalRef = null)
    {
        Status      = NotificationDeliveryStatus.Succeeded;
        ExternalRef = externalRef;
        CompletedAt = DateTime.UtcNow;
    }

    public void MarkFailed(string reason)
    {
        Status        = NotificationDeliveryStatus.Failed;
        FailureReason = reason;
        CompletedAt   = DateTime.UtcNow;
    }
}
