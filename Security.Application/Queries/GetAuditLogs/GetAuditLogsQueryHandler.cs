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
        var pagedLogs = await auditLogRepository.GetPagedAsync(
            userId:      request.UserId,
            page:        request.Page,
            pageSize:    request.PageSize,
            actorUserId: request.ActorUserId,
            action:      request.Action,
            from:        request.From,
            to:          request.To,
            ct:          ct);

        var dtos = pagedLogs.Items
            .Select(a => new AuditLogDto(
                Id:           a.Id,
                UserId:       a.UserId,
                ActorUserId:  a.ActorUserId,
                Action:       a.Action,
                ResourceType: a.ResourceType,
                ResourceId:   a.ResourceId,
                IpAddress:    a.IpAddress,
                Reason:       a.Reason,
                Metadata:     a.Metadata,
                OccurredAt:   a.OccurredAt))
            .ToList();

        return Result<PaginatedResult<AuditLogDto>>.Success(
            new PaginatedResult<AuditLogDto>(dtos, pagedLogs.TotalCount, request.Page, request.PageSize));
    }
}
