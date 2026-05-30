namespace Security.Contracts.Abstractions;

public sealed record AdminAuditEntry(
    Guid ActorUserId,
    Guid TargetUserId,
    string Action,
    string? Reason = null,
    string? Metadata = null,
    string? IpAddress = null);
