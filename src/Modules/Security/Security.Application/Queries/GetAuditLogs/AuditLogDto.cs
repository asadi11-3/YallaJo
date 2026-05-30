namespace Security.Application.Queries.GetAuditLogs;

public sealed record AuditLogDto(
    Guid Id,
    Guid? UserId,
    Guid? ActorUserId,
    string Action,
    string ResourceType,
    Guid? ResourceId,
    string? IpAddress,
    string? Reason,
    string? Metadata,
    DateTime OccurredAt);
