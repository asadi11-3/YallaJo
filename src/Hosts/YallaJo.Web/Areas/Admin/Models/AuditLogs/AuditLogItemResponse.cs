namespace YallaJo.Web.Areas.Admin.Models.AuditLogs;

/// <summary>
/// Phase 5A — Web mirror of the backend
/// <c>Security.Application.Queries.GetAuditLogs.AuditLogDto</c> projection.
/// <para>
/// <see cref="UserId"/> is the SUBJECT/TARGET of the audited action.
/// <see cref="ActorUserId"/> (Phase 4) is the admin who performed it
/// for admin lifecycle rows; null on legacy rows (REGISTER, LOGIN,
/// LOGOUT, PASSWORD_CHANGED, PASSWORD_RESET) where the action originated
/// from the subject themselves or from a system path.
/// </para>
/// <para>
/// <see cref="Reason"/> and <see cref="Metadata"/> are populated only
/// for admin lifecycle rows. The metadata payload is a compact JSON
/// document whose schema is per-action (see <c>Security.Contracts.Abstractions.AuditActions</c>);
/// the Web layer renders it as collapsible pretty-printed JSON and does
/// not interpret the schema.
/// </para>
/// </summary>
public sealed class AuditLogItemResponse
{
    public Guid     Id           { get; init; }
    public Guid?    UserId       { get; init; }
    public Guid?    ActorUserId  { get; init; }
    public string   Action       { get; init; } = string.Empty;
    public string   ResourceType { get; init; } = string.Empty;
    public Guid?    ResourceId   { get; init; }
    public string?  IpAddress    { get; init; }
    public string?  Reason       { get; init; }
    public string?  Metadata     { get; init; }
    public DateTime OccurredAt   { get; init; }
}
