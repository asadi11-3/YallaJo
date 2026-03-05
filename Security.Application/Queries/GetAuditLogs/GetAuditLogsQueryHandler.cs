// Security.Application/Queries/GetAuditLogs/GetAuditLogsQueryHandler.cs
using Security.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Application.Queries.GetAuditLogs;

public sealed class GetAuditLogsQueryHandler(IAuditLogRepository auditLogRepository)
    : IQueryHandler<GetAuditLogsQuery, PaginatedResult<AuditLogDto>>
{
    public async Task<Result<PaginatedResult<AuditLogDto>>> Handle(
        GetAuditLogsQuery request, CancellationToken ct)
    {
        var (items, total) = await auditLogRepository.GetPagedAsync(
            request.UserId, request.Page, request.PageSize, ct);

        var dtos = items
            .Select(a => new AuditLogDto(a.Id, a.UserId, a.Action, a.IpAddress, a.OccurredAt))
            .ToList();

        return Result<PaginatedResult<AuditLogDto>>.Success(
            new PaginatedResult<AuditLogDto>(dtos, total, request.Page, request.PageSize));
    }
}
