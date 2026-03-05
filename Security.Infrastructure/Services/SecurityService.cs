using Security.Application.Interfaces;
using Security.Contracts.Abstractions;
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
            e => e.Address == email.Trim().ToLowerInvariant() && e.IsPrimary);

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
            Claims: claims);
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
            Claims: claims);
    }

    public async Task<string?> GetPrimaryPhoneNumberAsync(Guid userId, CancellationToken ct = default)
    {
        return await userRepository.GetPrimaryPhoneNumberAsync(userId, ct);
    }

    public async Task<bool> ResetPasswordAsync(Guid userId, string newPassword, CancellationToken ct = default)
    {
        var user = await userRepository.GetByIdAsync(userId, ct, asNoTracking: false);
        if (user is null)
            return false;

        user.ResetPassword(passwordHasher.Hash(newPassword));
      

        await unitOfWork.SaveChangesAsync(ct);
        return true;
    }
}
