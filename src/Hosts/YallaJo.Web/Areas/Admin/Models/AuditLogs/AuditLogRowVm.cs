namespace YallaJo.Web.Areas.Admin.Models.AuditLogs;

/// <summary>
/// Phase 5A — Web view-model mirror of the API audit projection. New
/// nullable fields (<see cref="ActorUserId"/>, <see cref="ResourceType"/>,
/// <see cref="ResourceId"/>, <see cref="Reason"/>, <see cref="Metadata"/>)
/// are populated only for admin lifecycle rows; legacy rows leave them
/// null and the view falls back to "—".
/// </summary>
public sealed class AuditLogRowVm
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
