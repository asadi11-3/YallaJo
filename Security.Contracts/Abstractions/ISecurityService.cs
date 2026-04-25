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
