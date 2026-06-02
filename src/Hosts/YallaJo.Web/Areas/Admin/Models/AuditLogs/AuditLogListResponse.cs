namespace YallaJo.Web.Areas.Admin.Models.AuditLogs;

public sealed class AuditLogListResponse
{
     public IReadOnlyList<AuditLogItemResponse> Items { get; init; } = [];
     public int  PageNumber      { get; init; }
     public int  PageSize        { get; init; }
     public int  TotalCount      { get; init; }
     public bool HasPreviousPage { get; init; }
     public bool HasNextPage     { get; init; }
}
