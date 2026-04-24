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
    /// </summary>
    Task<bool> ReplacePasswordBySelfAsync(Guid userId, string newPassword, CancellationToken ct = default);

    /// <summary>
    /// Legacy, actor-blind password reset primitive. Retained for backward
    /// compatibility while callers migrate to the actor-attributed verbs
    /// (<see cref="ReplacePasswordBySelfAsync"/> and — in Phase 2+ —
    /// <c>ResetPasswordByAdminAsync</c>).
    /// </summary>
    [Obsolete("Use ReplacePasswordBySelfAsync for self-service recovery. An admin-initiated variant will be introduced in Phase 2.")]
    Task<bool> ResetPasswordAsync(Guid userId, string newPassword, CancellationToken ct = default);
}

public sealed record SecurityUserData(
    Guid UserId,
    string Email,
    bool IsEmailVerified,
    IReadOnlyList<string> Roles,
    IReadOnlyList<(string Type, string Value)> Claims);

public sealed record SecurityContactData(
    string Email,
    string? PhoneNumber);

/// <summary>
/// Lightweight lifecycle snapshot used to gate recovery flows. Mirrors the
/// fields <see cref="InviteAccountStatus"/> already carries for onboarding,
/// but is scoped to post-activation recovery semantics.
/// </summary>
public sealed record AccountStatus(
    Guid UserId,
    string Email,
    bool IsActive,
    bool IsEmailVerified);
