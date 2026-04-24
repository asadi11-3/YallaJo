using YallaJo.SharedKernel.Domain.Entities;

namespace Security.Domain.Entities;

/// <summary>
/// Append-only audit trail. Never soft-deleted.
/// <para>
/// <see cref="UserId"/> is the SUBJECT/TARGET of the action (nullable
/// because some system actions have no subject). It has no FK
/// constraint — cross-module references are by convention.
/// </para>
/// <para>
/// Phase 4 added three nullable columns: <see cref="ActorUserId"/>
/// (admin who performed the action), <see cref="Reason"/> (admin-facing
/// free-text reason), and <see cref="Metadata"/> (compact JSON payload
/// describing the mutation). All are nullable so existing audit
/// writers (REGISTER, LOGIN, LOGOUT, PASSWORD_CHANGED, PASSWORD_RESET)
/// continue to work unchanged via <see cref="Create"/>; admin lifecycle
/// verbs use <see cref="CreateAdmin"/>.
/// </para>
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

    /// <summary>
    /// Phase 4 — admin actor who performed the action. Null for
    /// self-service / system events recorded via <see cref="Create"/>.
    /// </summary>
    public Guid? ActorUserId { get; private set; }

    /// <summary>
    /// Phase 4 — admin-facing free-text reason supplied with the
    /// admin command. Trimmed; max 500 chars enforced by EF
    /// configuration. Never contains secrets.
    /// </summary>
    public string? Reason { get; private set; }

    /// <summary>
    /// Phase 4 — compact JSON payload describing the mutation
    /// (oldEmail/newEmail/lifecycleFrom/lifecycleTo/counts/etc.).
    /// Schema is conventional, not enforced at the column level.
    /// </summary>
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

    /// <summary>
    /// Phase 4 — factory for admin lifecycle audit rows. Records the
    /// admin <paramref name="actorUserId"/> alongside the
    /// <paramref name="targetUserId"/> (stored in <see cref="UserId"/>
    /// to keep existing per-user query indexes useful), an action verb
    /// drawn from the <c>AuditActions</c> catalog in
    /// <c>Security.Contracts</c>, an optional admin-facing
    /// <paramref name="reason"/>, and a pre-serialized JSON
    /// <paramref name="metadata"/> payload.
    /// </summary>
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
