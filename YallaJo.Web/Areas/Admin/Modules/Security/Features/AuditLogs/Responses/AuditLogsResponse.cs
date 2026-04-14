namespace YallaJo.Web.Areas.Admin.Modules.Security.Features.AuditLogs.Responses;

/// <summary>Mirrors PaginatedResult&lt;AuditLogDto&gt; from GET /api/v1/security/audit-logs.</summary>
public sealed class AuditLogListResponse
{
    public IReadOnlyList<AuditLogItemResponse> Items { get; init; } = [];
    public int  PageNumber      { get; init; }
    public int  PageSize        { get; init; }
    public int  TotalCount      { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage     { get; init; }
}

/// <summary>Mirrors AuditLogDto from Security.Application.</summary>
public sealed class AuditLogItemResponse
{
    public Guid      Id          { get; init; }
    public Guid?     UserId      { get; init; }
    public string    Action      { get; init; } = string.Empty;
    public string?   IpAddress   { get; init; }
    public DateTime  OccurredAt  { get; init; }
}
