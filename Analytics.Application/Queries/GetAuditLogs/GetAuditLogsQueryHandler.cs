using Analytics.Application.Interfaces.Repositories;
using Analytics.Application.Models;
using Analytics.Domain.Enums;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Analytics.Application.Queries.GetAuditLogs;

public sealed class GetAuditLogsQueryHandler(IAuditLogRepository repo, ILogger<GetAuditLogsQueryHandler> logger) : IQueryHandler<GetAuditLogsQuery, CursorPageDto<AdminAuditLogDto>>
{
    public async Task<Result<CursorPageDto<AdminAuditLogDto>>> Handle(GetAuditLogsQuery request, CancellationToken ct)
    {
        AuditLogAction? action = Enum.TryParse<AuditLogAction>(request.Action, true, out var a) ? a : null;
        var page = await repo.GetPageAsync(request.EntityType, request.EntityId, request.UserId, action, request.From, request.To, request.AfterId, request.PageSize, ct);
        logger.LogDebug("Read {Count} audit logs", page.Items.Count);
        return Result.Success(new CursorPageDto<AdminAuditLogDto>(page.Items.Select(x => new AdminAuditLogDto(x.Id, x.UserId, x.Action.ToString(), x.EntityType, x.EntityId, x.OccurredAt, x.RedactedAt)).ToList(), page.NextId));
    }
}
