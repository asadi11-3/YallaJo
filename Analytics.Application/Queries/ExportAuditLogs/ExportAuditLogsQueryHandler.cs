using Analytics.Application.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Analytics.Application.Queries.ExportAuditLogs;

public sealed class ExportAuditLogsQueryHandler(IAuditLogRepository repo, ILogger<ExportAuditLogsQueryHandler> logger) : IQueryHandler<ExportAuditLogsQuery, string>
{
    public async Task<Result<string>> Handle(ExportAuditLogsQuery request, CancellationToken ct)
    {
        var count = await repo.CountAsync(request.From, request.To, ct);
        if (count > 100_000) return Result.Failure<string>(new Error("AuditLog.ExportTooLarge", "Audit export exceeds 100,000 rows."), Outcome.UnprocessableEntity);
        logger.LogDebug("Exporting {Count} audit logs", count);
        return Result<string>.Success("Id,UserId,Action,EntityType,EntityId,OccurredAt\n");
    }
}
