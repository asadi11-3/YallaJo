using YallaJo.SharedKernel.Domain.Entities;

namespace Security.Domain.Entities;

public sealed class AuditLog : BaseEntity
{
    private AuditLog() { } // EF Core

    public Guid? UserId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string ResourceType { get; private set; } = string.Empty;
    public Guid? ResourceId { get; private set; }
    public string? IpAddress { get; private set; }
    public string? OldValue { get; private set; }
    public string? NewValue { get; private set; }
    public DateTime OccurredAt { get; private set; }

    public Guid? ActorUserId { get; private set; }

    public string? Reason { get; private set; }

    public string? Metadata { get; private set; }

    public static AuditLog Create(
        Guid? userId,
        string action,
        string resourceType,
        Guid? resourceId = null,
        string? ipAddress = null,
        string? oldValue = null,
        string? newValue = null)
    {
        return new AuditLog
        {
            UserId = userId,
            Action = action,
            ResourceType = resourceType,
            ResourceId = resourceId,
            IpAddress = ipAddress,
            OldValue = oldValue,
            NewValue = newValue,
            OccurredAt = DateTime.UtcNow
        };
    }

    public static AuditLog CreateAdmin(
        Guid actorUserId,
        Guid targetUserId,
        string action,
        string resourceType,
        Guid? resourceId = null,
        string? ipAddress = null,
        string? reason = null,
        string? metadata = null)
    {
        if (actorUserId == Guid.Empty)
            throw new ArgumentException("Actor user id is required.", nameof(actorUserId));
        if (targetUserId == Guid.Empty)
            throw new ArgumentException("Target user id is required.", nameof(targetUserId));
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action is required.", nameof(action));
        if (string.IsNullOrWhiteSpace(resourceType))
            throw new ArgumentException("Resource type is required.", nameof(resourceType));

        var trimmedReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();

        return new AuditLog
        {
            ActorUserId  = actorUserId,
            UserId       = targetUserId,
            Action       = action,
            ResourceType = resourceType,
            ResourceId   = resourceId,
            IpAddress    = ipAddress,
            Reason       = trimmedReason,
            Metadata     = metadata,
            OccurredAt   = DateTime.UtcNow
        };
    }
}
