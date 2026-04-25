namespace Security.Contracts.Abstractions;

public interface IAdminAuditWriter
{
    Task RecordAsync(AdminAuditEntry entry, CancellationToken cancellationToken = default);
}

public sealed record AdminAuditEntry(
    Guid ActorUserId,
    Guid TargetUserId,
    string Action,
    string? Reason = null,
    string? Metadata = null,
    string? IpAddress = null);

public static class AuditActions
{
    /// <summary>Resource type used for every admin lifecycle audit row.</summary>
    public const string UserResourceType = "User";

    public const string AdminResetPasswordInitiated = "ADMIN_RESET_PASSWORD_INITIATED";
    public const string AdminSuspendUser            = "ADMIN_SUSPEND_USER";
    public const string AdminReactivateUser         = "ADMIN_REACTIVATE_USER";
    public const string AdminArchiveUser            = "ADMIN_ARCHIVE_USER";
    public const string AdminReassignAccount        = "ADMIN_REASSIGN_ACCOUNT";
}
