using System.Linq.Expressions;
using Security.Domain.Entities;
using Security.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Application.Queries.GetAuditLogs;

public sealed class GetAuditLogsQueryHandler(IAuditLogRepository auditLogRepository)
    : IQueryHandler<GetAuditLogsQuery, PagedAuditLogsResponse>
{
    public async Task<Result<PagedAuditLogsResponse>> Handle(
        GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var action = string.IsNullOrWhiteSpace(request.Action) ? null : request.Action;


        var pagedLogs = await auditLogRepository.SelectPaginatedAsync(
            request.Page,
            request.PageSize,
            GetAuditLogsQueryExpressions.Selector,
            GetAuditLogsQueryExpressions.Filter(request, action),
            query => query.OrderByDescending(auditLog => auditLog.OccurredAt),
            cancellationToken);

        return Result<PagedAuditLogsResponse>.Success(
            new PagedAuditLogsResponse(
                pagedLogs.Items,
                request.Page,
                request.PageSize,
                pagedLogs.TotalCount));
    }
}
