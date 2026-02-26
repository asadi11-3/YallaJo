using Messaging.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;

namespace Messaging.Domain.Entities;

public sealed class NotificationTemplate : AuditableEntity
{
    private NotificationTemplate() { } // EF Core

    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public NotificationType Type { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public string? Subject { get; private set; }
    public string BodyTemplate { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
}
