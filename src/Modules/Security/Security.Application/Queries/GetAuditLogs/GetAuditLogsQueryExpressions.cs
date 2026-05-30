using System.Linq.Expressions;
using Security.Domain.Entities;

namespace Security.Application.Queries.GetAuditLogs;

internal static class GetAuditLogsQueryExpressions
{
    public static Expression<Func<AuditLog, AuditLogDto>> Selector =>
        auditLog => new AuditLogDto(
            auditLog.Id,
            auditLog.UserId,
            auditLog.ActorUserId,
            auditLog.Action,
            auditLog.ResourceType,
            auditLog.ResourceId,
            auditLog.IpAddress,
            auditLog.Reason,
            auditLog.Metadata,
            auditLog.OccurredAt);

    public static Expression<Func<AuditLog, bool>> Filter(GetAuditLogsQuery request, string? action)
    {
        return auditLog =>
            (!request.UserId.HasValue || auditLog.UserId == request.UserId.Value)
            && (!request.ActorUserId.HasValue || auditLog.ActorUserId == request.ActorUserId.Value)
            && (action == null || auditLog.Action == action)
            && (!request.From.HasValue || auditLog.OccurredAt >= request.From.Value)
            && (!request.To.HasValue || auditLog.OccurredAt <= request.To.Value);
    }
}
