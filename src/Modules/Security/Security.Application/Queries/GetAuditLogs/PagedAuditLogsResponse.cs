namespace Security.Application.Queries.GetAuditLogs;

public sealed record PagedAuditLogsResponse(
    IReadOnlyList<AuditLogDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    bool HasPreviousPage,
    bool HasNextPage)
{
    public PagedAuditLogsResponse(
        IReadOnlyList<AuditLogDto> items,
        int pageNumber,
        int pageSize,
        int totalCount)
        : this(
            items,
            pageNumber,
            pageSize,
            totalCount,
            pageNumber > 1,
            pageNumber * pageSize < totalCount)
    {
    }
}
