using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Contracts.Abstractions;
public interface ISecurityService
{
    Task<Guid?> GetUserIdByEmailAsync(string normalizedEmail, CancellationToken ct = default);
    Task<bool> MarkEmailVerifiedAsync(Guid userId, string email, CancellationToken ct = default);
    Task<SecurityUserData?> VerifyCredentialsAsync(string normalizedEmail, string password, CancellationToken ct = default);
    Task<SecurityUserData?> GetUserDataByIdAsync(Guid userId, CancellationToken ct = default);
    Task<string?> GetPrimaryPhoneNumberAsync(Guid userId, CancellationToken ct = default);
    Task<SecurityContactData?> GetPrimaryContactDataAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Returns lightweight lifecycle information about the account identified
    /// by the given (normalized) email, or <c>null</c> if no such account
    /// exists. Used by Auth's recovery flows to gate issuance of
    /// password-reset codes on <c>IsActive + IsEmailVerified</c> — accounts
    /// that have never been activated (or that are currently suspended) MUST
    /// NOT receive recovery codes.
    /// <para>
    /// Intentionally separate from <see cref="GetUserIdByEmailAsync"/> so
    /// callers can reason explicitly about lifecycle eligibility without
    /// loading full user state.
    /// </para>
    /// </summary>
    Task<AccountStatus?> GetAccountStatusByEmailAsync(
        string normalizedEmail,
        CancellationToken ct = default);

    /// <summary>
    /// User-initiated password replacement (self-service forgot-password flow).
    /// Raises <c>PasswordResetEvent</c> in the Security domain. Callers are
    /// responsible for invalidating any outstanding sessions / refresh tokens
    /// for the user in the same unit of work.
    /// <para>
    /// Phase 3A — on success, if the user's lifecycle state is
    /// <see cref="AccountLifecycleSnapshot.PendingPasswordReset"/> the
    /// Security side automatically clears it back to
    /// <see cref="AccountLifecycleSnapshot.Active"/>. This closes the loop
    /// for admin-initiated resets: admin moves the user to
    /// <c>PendingPasswordReset</c> at issue time, and the user's completion
    /// of the reset lands them back in <c>Active</c> atomically with the
    /// password mutation.
    /// </para>
    /// </summary>
    Task<bool> ReplacePasswordBySelfAsync(Guid userId, string newPassword, CancellationToken ct = default);

    /// <summary>
    /// Phase 3A — one-shot eligibility probe for the admin-initiated
    /// password-reset flow. Loads the target user, runs the
    /// <c>IRoleHierarchyService.EnsureCanManageUserAsync</c> check against
    /// the actor (which also denies privileged self-management), and
    /// returns a lightweight snapshot of the fields the Auth-side command
    /// needs to issue the reset token:
    /// <list type="bullet">
    ///   <item><description>Primary email (delivery address for the reset email).</description></item>
    ///   <item><description>Primary-email verification flag (reset cannot be sent to an unverified address).</description></item>
    ///   <item><description>Lifecycle snapshot so the Auth handler can gate on <c>Active</c> / <c>PendingPasswordReset</c>.</description></item>
    /// </list>
    /// <para>
    /// Result mapping:
    /// <list type="bullet">
    ///   <item><description><c>NotFound</c> — target user does not exist.</description></item>
    ///   <item><description><c>Forbidden</c> — actor lacks rank (or is the target).</description></item>
    ///   <item><description><c>Success</c> — returns the snapshot; Auth applies the lifecycle/email gates itself.</description></item>
    /// </list>
    /// </para>
    /// </summary>
    Task<Result<AdminResetEligibility>> GetAdminResetEligibilityAsync(
        Guid targetUserId,
        Guid actorUserId,
        CancellationToken ct = default);

    /// <summary>
    /// Phase 3B — admin lifecycle operation. Suspends a target account after
    /// hierarchy/self-management checks.
    /// </summary>
    /// <remarks>
    /// Allowed source states: Active, PendingPasswordReset, Suspended (idempotent).
    /// Rejected source states: Provisioned, PendingActivation, Archived.
    /// </remarks>
    Task<Result> SuspendUserByAdminAsync(
        Guid targetUserId,
        Guid actorUserId,
        CancellationToken ct = default);

    /// <summary>
    /// Phase 3B — admin lifecycle operation. Reactivates a suspended account
    /// after hierarchy/self-management checks.
    /// </summary>
    /// <remarks>
    /// Allowed source state: Suspended.
    /// </remarks>
    Task<Result> ReactivateUserByAdminAsync(
        Guid targetUserId,
        Guid actorUserId,
        CancellationToken ct = default);

    /// <summary>
    /// Phase 3B — admin lifecycle operation. Archives a target account (terminal
    /// state) after hierarchy/self-management checks.
    /// </summary>
    Task<Result> ArchiveUserByAdminAsync(
        Guid targetUserId,
        Guid actorUserId,
        CancellationToken ct = default);

    /// <summary>
    /// Phase 3C — admin reassignment. Retargets the target account's
    /// primary email to <paramref name="newEmail"/>, invalidates the
    /// password with an unusable placeholder hash, resets email
    /// verification, and transitions lifecycle to
    /// <see cref="AccountLifecycleSnapshot.PendingActivation"/>. All
    /// mutations happen via domain methods (<c>User.ReassignToPendingActivation</c>).
    /// <para>
    /// Outcome mapping:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><c>Unauthorized</c> — actor not authenticated.</description></item>
    ///   <item><description><c>Forbidden</c> — hierarchy / self-management denial.</description></item>
    ///   <item><description><c>NotFound</c> — target user does not exist.</description></item>
    ///   <item><description><c>Conflict</c> — current lifecycle is not eligible (Provisioned, PendingActivation, Archived), or <paramref name="newEmail"/> is already in use, or the account has no primary email.</description></item>
    ///   <item><description><c>Success</c> — returns the eligibility snapshot (old email, new email, actor id) so the Auth-side handler can supersede tokens, deactivate external providers, revoke sessions, and issue a fresh activation token in its own unit of work.</description></item>
    /// </list>
    /// <para>
    /// The Security side does NOT touch Auth aggregates (tokens,
    /// sessions, external providers). The Auth command orchestrates
    /// both modules under an <c>ITransactionalExecutor</c>.
    /// </para>
    /// </summary>
    Task<Result<ReassignmentCompleted>> ReassignUserByAdminAsync(
        Guid targetUserId,
        Guid actorUserId,
        string newEmail,
        CancellationToken ct = default);

    /// <summary>
    /// Legacy, actor-blind password reset primitive. Retained for backward
    /// compatibility while callers migrate to the actor-attributed verbs
    /// (<see cref="ReplacePasswordBySelfAsync"/> and — in Phase 2+ —
    /// <c>ResetPasswordByAdminAsync</c>).
    /// </summary>
    [Obsolete("Use ReplacePasswordBySelfAsync for self-service recovery. An admin-initiated variant will be introduced in Phase 2C+.")]
    Task<bool> ResetPasswordAsync(Guid userId, string newPassword, CancellationToken ct = default);
}

/// <summary>
/// Phase 3A — snapshot returned by
/// <see cref="ISecurityService.GetAdminResetEligibilityAsync"/>. Lives in
/// <c>Security.Contracts</c> so Auth does not take a dependency on
/// <c>Security.Domain</c>.
/// </summary>
public sealed record AdminResetEligibility(
    Guid TargetUserId,
    string PrimaryEmail,
    bool IsPrimaryEmailVerified,
    AccountLifecycleSnapshot Lifecycle);

/// <summary>
/// Phase 3C — result payload for
/// <see cref="ISecurityService.ReassignUserByAdminAsync"/>. Carries the
/// addresses and lifecycle snapshot the Auth-side handler needs to
/// supersede tokens, deactivate external providers, revoke sessions,
/// and issue a fresh activation email to the new address.
/// </summary>
public sealed record ReassignmentCompleted(
    Guid TargetUserId,
    string OldEmail,
    string NewEmail,
    AccountLifecycleSnapshot Lifecycle);

public sealed record SecurityUserData(
    Guid UserId,
    string Email,
    bool IsEmailVerified,
    IReadOnlyList<string> Roles,
    IReadOnlyList<(string Type, string Value)> Claims,
    // Phase 2A: defaults to Active so pre-Phase-2A constructions (test
    // doubles, downstream consumers) keep compiling. The Security
    // infrastructure ALWAYS populates this field with the real value;
    // the default exists only to keep the contract source-compatible
    // during the transition.
    AccountLifecycleSnapshot Lifecycle = AccountLifecycleSnapshot.Active);

public sealed record SecurityContactData(
    string Email,
    string? PhoneNumber);

/// <summary>
/// Lightweight lifecycle snapshot used to gate recovery and login flows.
/// <see cref="IsActive"/> is preserved as a derived convenience for backward
/// compatibility with Phase 1 callers; new code should branch on
/// <see cref="Lifecycle"/> directly.
/// </summary>
public sealed record AccountStatus(
    Guid UserId,
    string Email,
    bool IsActive,
    bool IsEmailVerified,
    // Phase 2A: defaults to a derived guess so pre-Phase-2A test doubles
    // continue to compile. Production code in SecurityService always
    // supplies the real value sourced from User.LifecycleState.
    AccountLifecycleSnapshot Lifecycle = AccountLifecycleSnapshot.Active);

/// <summary>
/// Cross-module-safe projection of <c>Security.Domain.Entities.AccountLifecycleState</c>.
/// <para>
/// Defined in <c>Security.Contracts</c> so consumers (Auth, future modules)
/// do not need a project reference on <c>Security.Domain</c>. The ordinals
/// are guaranteed to match the domain enum 1:1 — see Phase 2A commit notes
/// for the mapping discipline (the Security infrastructure layer performs
/// the cast at the contract boundary).
/// </para>
/// </summary>
public enum AccountLifecycleSnapshot
{
    Provisioned          = 0,
    PendingActivation    = 1,
    Active               = 2,
    Suspended            = 3,
    PendingPasswordReset = 4,
    Archived             = 5,
}
