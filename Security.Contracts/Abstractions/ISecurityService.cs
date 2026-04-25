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

    Task<AccountStatus?> GetAccountStatusByEmailAsync(string normalizedEmail, CancellationToken ct = default);

    Task<bool> ReplacePasswordBySelfAsync(Guid userId, string newPassword, CancellationToken ct = default);

    Task<Result<AdminResetEligibility>> GetAdminResetEligibilityAsync(
        Guid targetUserId,
        Guid actorUserId,
        CancellationToken ct = default);

    Task<Result> SuspendUserByAdminAsync(
        Guid targetUserId,
        Guid actorUserId,
        CancellationToken ct = default);

    Task<Result> ReactivateUserByAdminAsync(
        Guid targetUserId,
        Guid actorUserId,
        CancellationToken ct = default);

    Task<Result> ArchiveUserByAdminAsync(
        Guid targetUserId,
        Guid actorUserId,
        CancellationToken ct = default);

    Task<Result<ReassignmentCompleted>> ReassignUserByAdminAsync(
        Guid targetUserId,
        Guid actorUserId,
        string newEmail,
        CancellationToken ct = default);

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
