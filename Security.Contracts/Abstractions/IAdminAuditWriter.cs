namespace Security.Contracts.Abstractions;

/// <summary>
/// Phase 4 — cross-module write capability for the admin audit
/// timeline. Exposed by Security so Auth's admin lifecycle command
/// handlers can record successful admin actions (suspend, reactivate,
/// archive, reset-password, reassign) without taking a dependency on
/// <c>Security.Domain</c> or the <c>SecurityDbContext</c>.
/// <para>
/// Contract semantics:
/// </para>
/// <list type="bullet">
///   <item><description>Callers invoke <see cref="RecordAsync"/> only on the success path of an admin command — failures (authn, authz, lifecycle, validation) MUST NOT write a row.</description></item>
///   <item><description>The implementation appends a row to <c>security.AuditLogs</c> and saves on the Security UoW. When invoked inside an ambient <c>TransactionScope</c> (e.g. the reassignment flow), the save enlists in that scope and rolls back with the rest of the transaction.</description></item>
///   <item><description>Plain secrets (activation tokens, reset codes, password hashes, JWTs) MUST NEVER appear in <see cref="AdminAuditEntry.Metadata"/> or <see cref="AdminAuditEntry.Reason"/>. The writer does not redact — producers are responsible.</description></item>
/// </list>
/// </summary>
public interface IAdminAuditWriter
{
    Task RecordAsync(AdminAuditEntry entry, CancellationToken cancellationToken = default);
}

/// <summary>
/// Phase 4 — input payload for <see cref="IAdminAuditWriter.RecordAsync"/>.
/// </summary>
/// <param name="ActorUserId">Admin who performed the action. Required.</param>
/// <param name="TargetUserId">User the action was performed on. Required.</param>
/// <param name="Action">
/// Action verb from <see cref="AuditActions"/>. The constant string is
/// the durable contract — do not refactor without coordinating with
/// downstream analytics consumers that read <c>security.AuditLogs</c>.
/// </param>
/// <param name="Reason">
/// Optional admin-facing free-text reason. Trimmed by the domain
/// factory; max 500 chars. Never contains secrets.
/// </param>
/// <param name="Metadata">
/// Optional pre-serialized JSON payload describing the mutation
/// (e.g. <c>{"oldEmail":"…","newEmail":"…","lifecycleFrom":"Active","lifecycleTo":"PendingActivation","profileScrubbed":true}</c>).
/// Schema is conventional per action — see action-specific docs in the
/// admin handler.
/// </param>
/// <param name="IpAddress">
/// Optional admin actor IP, captured from <c>IRequestContext</c>.
/// Stored without normalization; max 45 chars (IPv6).
/// </param>
public sealed record AdminAuditEntry(
    Guid ActorUserId,
    Guid TargetUserId,
    string Action,
    string? Reason = null,
    string? Metadata = null,
    string? IpAddress = null);

/// <summary>
/// Phase 4 — durable string constants for admin audit action verbs.
/// These values are persisted as-is in the <c>Action</c> column and
/// queried by analytics consumers; they are part of the public audit
/// contract and must not be renamed without a migration plan.
/// </summary>
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
