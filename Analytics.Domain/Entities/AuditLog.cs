using YallaJo.SharedKernel.Domain.Entities;

namespace Analytics.Domain.Entities;

public sealed class AuditLog : BaseEntity<long>, IAggregateRoot
{
    private AuditLog() { } // EF Core

    public Guid? UserId { get; private set; }
    public string? UserAgent { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public string? EntityId { get; private set; }
    public string? IpAddress { get; private set; }
    public string? OldValues { get; private set; }
    public string? NewValues { get; private set; }
    public DateTime OccurredAt { get; private set; }
}
