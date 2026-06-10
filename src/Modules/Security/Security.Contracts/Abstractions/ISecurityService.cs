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
    /// B6: whether the user holds a USABLE local password — i.e. their stored hash is a
    /// real ASP.NET Identity hash, not one of the intentional non-Base64 placeholders
    /// (<c>EXTERNAL-ONLY:</c> set for Google/Facebook auto-registrations,
    /// <c>REASSIGNED:</c> set by admin reassignment). Consumed by the Auth module's
    /// unlink-external-provider guard so a placeholder-password account cannot remove
    /// its only remaining sign-in method and lock itself out.
    /// Returns <c>false</c> for unknown users (fail closed).
    /// </summary>
    Task<bool> HasUsablePasswordAsync(Guid userId, CancellationToken ct = default);

    Task<AccountStatus?> GetAccountStatusByEmailAsync(string normalizedEmail, CancellationToken ct = default);

    Task<bool> ReplacePasswordBySelfAsync(Guid userId, string newPassword, CancellationToken ct = default);

    Task<Result<AdminResetEligibility>> GetAdminResetEligibilityAsync(
        Guid targetUserId,
        Guid actorUserId,
        CancellationToken ct = default);

    Task<Result> EnsureCanManageUserAsync(
        Guid actorUserId,
        Guid targetUserId,
        CancellationToken cancellationToken = default);

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
