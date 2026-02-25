namespace Security.Contracts.Abstractions;

/// <summary>
/// Cross-module contract: allows other modules (e.g. Auth) to perform
/// identity operations against the Security module without a direct project reference.
/// Implemented by Security.Infrastructure; consumed via DI.
/// </summary>
public interface ISecurityService
{
    /// <summary>
    /// Returns the user's Id if an account with the given normalized email exists.
    /// </summary>
    Task<Guid?> GetUserIdByEmailAsync(string normalizedEmail, CancellationToken ct = default);

    /// <summary>
    /// Marks the user's email as verified and activates the account.
    /// Returns true if verification succeeded; false if user/email not found.
    /// </summary>
    Task<bool> MarkEmailVerifiedAsync(Guid userId, string email, CancellationToken ct = default);

    /// <summary>
    /// Validates credentials and returns identity data needed for token generation.
    /// Returns null if email not found or password mismatch.
    /// </summary>
    Task<SecurityUserData?> VerifyCredentialsAsync(
        string normalizedEmail, string password, CancellationToken ct = default);
}

/// <summary>
/// DTO returned by <see cref="ISecurityService.VerifyCredentialsAsync"/> containing
/// the identity data the Auth module needs to issue tokens.
/// </summary>
public sealed record SecurityUserData(
    Guid UserId,
    string Email,
    bool IsEmailVerified,
    IReadOnlyList<string> Roles,
    IReadOnlyList<(string Type, string Value)> Claims);
