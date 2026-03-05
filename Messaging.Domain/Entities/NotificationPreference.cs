using Messaging.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;

namespace Messaging.Domain.Entities;

public sealed class NotificationPreference : AuditableEntity
{
    private NotificationPreference() { } // EF Core

    public Guid UserId { get; private set; }
    public NotificationType NotificationType { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public bool IsEnabled { get; private set; } = true;
}
