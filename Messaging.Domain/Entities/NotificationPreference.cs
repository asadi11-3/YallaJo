using Messaging.Domain.Enums;
using Messaging.Domain.Events;
using YallaJo.SharedKernel.Domain.Entities;

namespace Messaging.Domain.Entities;

public sealed class NotificationPreference : AuditableEntity, IAggregateRoot
{
    private NotificationPreference() { } // EF Core

    public Guid UserId { get; private set; }
    public NotificationType NotificationType { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public bool IsEnabled { get; private set; } = true;

    // ── Factory ───────────────────────────────────────────────────────────────

    public static NotificationPreference Create(
        Guid userId,
        NotificationType notificationType,
        NotificationChannel channel,
        bool isEnabled = true)
    {
        return new NotificationPreference
        {
            UserId           = userId,
            NotificationType = notificationType,
            Channel          = channel,
            IsEnabled        = isEnabled,
        };
    }

    // ── Business Methods ──────────────────────────────────────────────────────

    /// <summary>Updates the enabled flag. Throws if trying to disable a critical type.</summary>
    public void SetEnabled(bool enabled)
    {
        if (!enabled && NotificationType.IsCritical())
            throw new InvalidOperationException("NotificationPreference.CannotDisableCritical");

        if (IsEnabled == enabled) return; // idempotent
        IsEnabled = enabled;

        AddDomainEvent(new NotificationPreferenceUpdatedDomainEvent(
            Id, UserId, NotificationType, Channel, IsEnabled));
        MarkUpdated();
    }
}
