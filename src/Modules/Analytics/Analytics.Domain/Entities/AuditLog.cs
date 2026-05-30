using Analytics.Domain.Enums;
using Analytics.Domain.Events;
using YallaJo.SharedKernel.Domain.Entities;

namespace Analytics.Domain.Entities;

public sealed class AuditLog : BaseEntity<long>, IAggregateRoot
{
    private AuditLog() { }

    public Guid? UserId { get; private set; }
    public string? Username { get; private set; }
    public AuditLogAction Action { get; private set; }
    public string? CustomActionName { get; private set; }
    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public string? OldValue { get; private set; }
    public string? NewValue { get; private set; }
    public string? UserAgent { get; private set; }
    public string? IpAddressHash { get; private set; }
    public string? CorrelationId { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public DateTime? RedactedAt { get; private set; }
    public string? RedactionReason { get; private set; }
    public Guid? RedactedByUserId { get; private set; }

    public static AuditLog Append(Guid? userId, string? username, AuditLogAction action, string? customActionName, string entityType, Guid entityId, string? oldValue, string? newValue, string? userAgent, string? ipAddressHash, string? correlationId, DateTime now)
    {
        var entry = new AuditLog { UserId = userId, Username = username, Action = action, CustomActionName = customActionName, EntityType = entityType, EntityId = entityId, OldValue = oldValue, NewValue = newValue, UserAgent = userAgent, IpAddressHash = ipAddressHash, CorrelationId = correlationId, OccurredAt = now };
        entry.AddDomainEvent(new AuditLogEntryAppendedDomainEvent(entry.Id, userId, (int)action, entityType, entityId, now));
        return entry;
    }

    public void RetroactivelyRedact(Guid adminUserId, string reason, IReadOnlyList<string> redactedFields, string? redactedOldValue, string? redactedNewValue, DateTime now)
    {
        if (RedactedAt is not null) throw new InvalidOperationException("Already redacted");
        OldValue = redactedOldValue;
        NewValue = redactedNewValue;
        RedactedAt = now;
        RedactedByUserId = adminUserId;
        RedactionReason = reason;
        AddDomainEvent(new AuditLogEntryRedactedDomainEvent(Id, (int)Action, EntityType, EntityId, redactedFields));
    }
}
