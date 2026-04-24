namespace Security.Application.Queries.GetAuditLogs;

/// <summary>
/// Phase 4 — projection of <c>Security.AuditLog</c> for the admin
/// audit timeline read API. <see cref="UserId"/> is the SUBJECT of the
/// action; <see cref="ActorUserId"/> (Phase 4) is the admin who
/// performed it for admin lifecycle rows. <see cref="Reason"/> and
/// <see cref="Metadata"/> are populated only for admin rows; legacy
/// rows (REGISTER, LOGIN, LOGOUT, PASSWORD_CHANGED, PASSWORD_RESET)
/// leave them null.
/// </summary>
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
