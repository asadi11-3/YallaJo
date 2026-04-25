namespace YallaJo.Web.Areas.Admin.Modules.Security.Features.AuditLogs.ViewModels;

/// <summary>
/// Phase 5A — paginated audit-list view-model with the Phase 4 filter
/// surface. Existing single-filter callers (subject userId only) keep
/// working because every new filter property is nullable.
/// </summary>
public sealed class AuditLogListVm
{
    public IReadOnlyList<AuditLogRowVm> Logs { get; init; } = [];

    // Pagination
    public int  Page        { get; init; } = 1;
    public int  TotalCount  { get; init; }
    public bool HasPrevious { get; init; }
    public bool HasNext     { get; init; }

    // Filters (all optional; preserved across page navigation).
    public Guid?     FilterUserId      { get; init; }
    public Guid?     FilterActorUserId { get; init; }
    public string?   FilterAction      { get; init; }
    public DateTime? FilterFrom        { get; init; }
    public DateTime? FilterTo          { get; init; }
}
