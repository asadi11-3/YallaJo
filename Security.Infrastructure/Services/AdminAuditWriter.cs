using Microsoft.Extensions.Logging;
using Security.Contracts.Abstractions;
using Security.Domain.Entities;
using Security.Domain.Repositories;
using Security.Infrastructure.Persistence;

namespace Security.Infrastructure.Services;

internal sealed class AdminAuditWriter(
    SecurityDbContext dbContext,
    ISecurityUnitOfWork unitOfWork,
    ILogger<AdminAuditWriter> logger)
    : IAdminAuditWriter
{
    public async Task RecordAsync(AdminAuditEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var auditLog = AuditLog.CreateAdmin(
            actorUserId:  entry.ActorUserId,
            targetUserId: entry.TargetUserId,
            action:       entry.Action,
            resourceType: AuditActions.UserResourceType,
            resourceId:   entry.TargetUserId,
            ipAddress:    entry.IpAddress,
            reason:       entry.Reason,
            metadata:     entry.Metadata);

        dbContext.AuditLogs.Add(auditLog);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Security: AuditLog written — {Action} by admin {ActorUserId} on user {TargetUserId} (entry {EntryId}).",
            entry.Action,
            entry.ActorUserId,
            entry.TargetUserId,
            auditLog.Id);
    }
}
