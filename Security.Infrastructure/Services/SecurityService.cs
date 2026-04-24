using Security.Application.Interfaces;
using Security.Contracts.Abstractions;
using Security.Domain.Entities;
using Security.Domain.Repositories;

namespace Security.Infrastructure.Services;

internal sealed class SecurityService(
    IUserRepository userRepository,
    ISecurityUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher) : ISecurityService
{
    public async Task<Guid?> GetUserIdByEmailAsync(string normalizedEmail, CancellationToken ct = default)
    {
        return await userRepository.GetUserIdByEmailAsync(normalizedEmail, ct);
    }

    public async Task<bool> MarkEmailVerifiedAsync(Guid userId, string email, CancellationToken ct = default)
    {
        var user = await userRepository.GetByIdWithEmailsAsync(userId, ct);
        if (user is null)
            return false;

        var emailEntity = user.Emails.FirstOrDefault(
            e => e.Address.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase) && e.IsPrimary);

        if (emailEntity is null)
            return false;

        if (emailEntity.IsVerified)
            return true;

        user.VerifyEmail(emailEntity.Id);

        await unitOfWork.SaveChangesAsync(ct);
        return true;
    }

    public async Task<SecurityUserData?> VerifyCredentialsAsync(
        string normalizedEmail, string password, CancellationToken ct = default)
    {
        var user = await userRepository.GetByEmailWithDetailsAsync(normalizedEmail, ct);
        if (user is null)
            return null;

        if (!passwordHasher.Verify(password, user.PasswordHash))
            return null;

        var primaryEmail = user.GetPrimaryEmail();
        var isEmailVerified = primaryEmail?.IsVerified ?? false;

        // Materialize active roles
        var activeUserRoles = user.UserRoles
            .Where(ur => ur.Role.IsActive)
            .ToList();

        var roles = activeUserRoles
            .Select(ur => ur.Role.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Merge user claims + role claims, deduplicated
        var userClaims = user.UserClaims
            .Select(c => (c.ClaimType, c.ClaimValue));

        var roleClaims = activeUserRoles
            .SelectMany(ur => ur.Role.RoleClaims)
            .Select(c => (c.ClaimType, c.ClaimValue));

        var claims = userClaims
            .Concat(roleClaims)
            .DistinctBy(c => (c.ClaimType, c.ClaimValue))
            .ToList();

        return new SecurityUserData(
            UserId: user.Id,
            Email: normalizedEmail,
            IsEmailVerified: isEmailVerified,
            Roles: roles,
            Claims: claims,
            Lifecycle: ToContractSnapshot(user.LifecycleState));
    }

    public async Task<SecurityUserData?> GetUserDataByIdAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userRepository.GetByIdWithDetailsAsync(userId, ct);
        if (user is null)
            return null;

        var primaryEmail = user.GetPrimaryEmail();
        if (primaryEmail is null)
            return null;

        var isEmailVerified = primaryEmail.IsVerified;

        // Materialize active roles
        var activeUserRoles = user.UserRoles
            .Where(ur => ur.Role.IsActive)
            .ToList();

        var roles = activeUserRoles
            .Select(ur => ur.Role.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Merge user claims + role claims, deduplicated
        var userClaims = user.UserClaims
            .Select(c => (c.ClaimType, c.ClaimValue));

        var roleClaims = activeUserRoles
            .SelectMany(ur => ur.Role.RoleClaims)
            .Select(c => (c.ClaimType, c.ClaimValue));

        var claims = userClaims
            .Concat(roleClaims)
            .DistinctBy(c => (c.ClaimType, c.ClaimValue))
            .ToList();

        return new SecurityUserData(
            UserId: user.Id,
            Email: primaryEmail.Address,
            IsEmailVerified: isEmailVerified,
            Roles: roles,
            Claims: claims,
            Lifecycle: ToContractSnapshot(user.LifecycleState));
    }

    public async Task<string?> GetPrimaryPhoneNumberAsync(Guid userId, CancellationToken ct = default)
    {
        return await userRepository.GetPrimaryPhoneNumberAsync(userId, ct);
    }

    public async Task<SecurityContactData?> GetPrimaryContactDataAsync(Guid userId, CancellationToken ct = default)
    {
        // Email is sourced from the Security aggregate (persistent source of truth),
        // not from JWT claims. Phone is optional.
        var user = await userRepository.GetByIdWithEmailsAsync(userId, ct);
        if (user is null)
            return null;

        var primaryEmail = user.GetPrimaryEmail();
        if (primaryEmail is null)
            return null;

        var phoneNumber = await userRepository.GetPrimaryPhoneNumberAsync(userId, ct);

        return new SecurityContactData(primaryEmail.Address, phoneNumber);
    }

    public async Task<AccountStatus?> GetAccountStatusByEmailAsync(
        string normalizedEmail,
        CancellationToken ct = default)
    {
        // Reuse the same user-with-emails path the invite/onboarding flow
        // already uses — avoids introducing a new query shape for Phase 1.
        var userId = await userRepository.GetUserIdByEmailAsync(normalizedEmail, ct);
        if (userId is null)
            return null;

        var user = await userRepository.GetByIdWithEmailsAsync(userId.Value, ct);
        if (user is null)
            return null;

        var primary = user.GetPrimaryEmail();

        return new AccountStatus(
            UserId:          user.Id,
            Email:           primary?.Address ?? normalizedEmail,
            IsActive:        user.IsActive,
            IsEmailVerified: primary?.IsVerified ?? false,
            Lifecycle:       ToContractSnapshot(user.LifecycleState));
    }

    /// <summary>
    /// Maps the domain enum to its contract counterpart. Both enums are
    /// guaranteed ordinal-aligned (Phase 2A discipline), so the cast is safe;
    /// kept as a method so any future divergence has one place to break.
    /// </summary>
    private static AccountLifecycleSnapshot ToContractSnapshot(AccountLifecycleState state)
        => (AccountLifecycleSnapshot)(int)state;

    public async Task<bool> ReplacePasswordBySelfAsync(
        Guid userId,
        string newPassword,
        CancellationToken ct = default)
    {
        // Phase 1: same domain transition as the legacy ResetPasswordAsync —
        // this contract verb exists so audit / telemetry downstream can
        // distinguish self-service from (future) admin-initiated resets
        // without digging through event metadata. Phase 2 will introduce a
        // distinct User.ReplacePasswordBySelf domain method that raises a
        // dedicated event; for now the call still goes through User.ResetPassword
        // to keep behaviour bit-for-bit identical.
        var user = await userRepository.GetByIdAsync(userId, ct, asNoTracking: false);
        if (user is null)
            return false;

        user.ResetPassword(passwordHasher.Hash(newPassword));
        await unitOfWork.SaveChangesAsync(ct);
        return true;
    }

    [Obsolete("Use ReplacePasswordBySelfAsync for self-service recovery. An admin-initiated variant will be introduced in Phase 2.")]
    public Task<bool> ResetPasswordAsync(Guid userId, string newPassword, CancellationToken ct = default)
        => ReplacePasswordBySelfAsync(userId, newPassword, ct);
}
