using YallaJo.SharedKernel.Domain.Entities;

namespace Security.Domain.Entities;

/// <summary>
/// Append-only audit trail. Never soft-deleted.
/// UserId is nullable (system actions have no user) and has no FK constraint
/// (cross-module reference by convention).
/// </summary>
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
}
