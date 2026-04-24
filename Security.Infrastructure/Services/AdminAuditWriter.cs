using Microsoft.Extensions.Logging;
using Security.Contracts.Abstractions;
using Security.Domain.Entities;
using Security.Domain.Repositories;
using Security.Infrastructure.Persistence;

namespace Security.Infrastructure.Services;

/// <summary>
/// Phase 4 implementation of <see cref="IAdminAuditWriter"/>. Appends a
/// row to <c>security.AuditLogs</c> via the
/// <see cref="AuditLog.CreateAdmin"/> domain factory and saves on the
/// Security UoW.
/// <para>
/// Atomicity:
/// </para>
/// <list type="bullet">
///   <item><description>When called inside an ambient <c>TransactionScope</c> (e.g. <c>AdminReassignAccountCommandHandler</c> running under <c>ITransactionalExecutor</c>), the save enlists in that scope and rolls back with the rest of the transaction. No row appears if the outer scope fails to <c>Complete()</c>.</description></item>
///   <item><description>When called outside an ambient scope (suspend / reactivate / archive / admin-reset paths), the save commits immediately on the Security UoW. This is a deliberate two-step commit (matching the existing <c>AdminResetPasswordCommandHandler</c> posture for <c>MarkPendingPasswordResetAsync</c>) — accepted because the failure mode is benign: if Auth's subsequent session-revocation save fails, the audit row still correctly reflects what the admin requested and what Security committed.</description></item>
/// </list>
/// </summary>
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
