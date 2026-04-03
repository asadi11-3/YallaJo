namespace Security.Contracts.Abstractions;
public interface ISecurityService
{
    Task<Guid?> GetUserIdByEmailAsync(string normalizedEmail, CancellationToken ct = default);
    Task<bool> MarkEmailVerifiedAsync(Guid userId, string email, CancellationToken ct = default);
    Task<SecurityUserData?> VerifyCredentialsAsync(string normalizedEmail, string password, CancellationToken ct = default);
    Task<SecurityUserData?> GetUserDataByIdAsync(Guid userId, CancellationToken ct = default);
    Task<string?> GetPrimaryPhoneNumberAsync(Guid userId, CancellationToken ct = default);
    Task<bool> ResetPasswordAsync(Guid userId, string newPassword, CancellationToken ct = default);
}

public sealed record SecurityUserData(
    Guid UserId,
    string Email,
    bool IsEmailVerified,
    IReadOnlyList<string> Roles,
    IReadOnlyList<(string Type, string Value)> Claims);
