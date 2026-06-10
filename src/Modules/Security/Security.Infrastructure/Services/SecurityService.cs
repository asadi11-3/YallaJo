using Microsoft.Extensions.Caching.Hybrid;
using Security.Application.Caching;
using Security.Application.Authorization;
using Security.Application.Interfaces;
using Security.Domain.Entities;
using Security.Domain.Errors;
using Security.Domain.Repositories;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Security.Contracts.Abstractions;

namespace Security.Infrastructure.Services;

internal sealed class SecurityService(
    IUserRepository userRepository,
    ISecurityUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IRoleHierarchyService roleHierarchy,
    HybridCache cache) : ISecurityService
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

    public async Task<bool> HasUsablePasswordAsync(Guid userId, CancellationToken ct = default)
    {
        var hash = await userRepository.GetPasswordHashAsync(userId, ct);

        // Mirrors PasswordHasher.Verify's fail-closed semantics: a missing user, an
        // empty hash, or a non-Base64 value (the deliberate "EXTERNAL-ONLY:<guid>" /
        // "REASSIGNED:<guid>" placeholders) all mean "no usable local password".
        if (string.IsNullOrWhiteSpace(hash))
            return false;

        var buffer = new byte[((hash.Length * 3) + 3) / 4];
        return Convert.TryFromBase64String(hash, buffer, out _);
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
            UserId: user.Id,
            Email: primary?.Address ?? normalizedEmail,
            IsActive: user.IsActive,
            IsEmailVerified: primary?.IsVerified ?? false,
            Lifecycle: ToContractSnapshot(user.LifecycleState));
    }

    private static AccountLifecycleSnapshot ToContractSnapshot(AccountLifecycleState state)
        => (AccountLifecycleSnapshot)(int)state;

    public async Task<bool> ReplacePasswordBySelfAsync(
        Guid userId,
        string newPassword,
        CancellationToken ct = default)
    {
        var user = await userRepository.GetByIdAsync(userId, ct, asNoTracking: false);
        if (user is null)
            return false;

        user.ResetPassword(passwordHasher.Hash(newPassword));

        if (user.LifecycleState == AccountLifecycleState.PendingPasswordReset)
        {
            user.Activate();
        }

        await unitOfWork.SaveChangesAsync(ct);
        return true;
    }

    public async Task<Result<AdminResetEligibility>> GetAdminResetEligibilityAsync(
        Guid targetUserId,
        Guid actorUserId,
        CancellationToken ct = default)
    {
        _ = actorUserId;

        var hierarchy = await roleHierarchy.EnsureCanManageUserAsync(targetUserId, ct);
        if (hierarchy.IsFailure)
        {
            // Propagate the service's outcome (Forbidden on hierarchy
            // denial, Unauthorized if the actor is not authenticated).
            return Result<AdminResetEligibility>.Fail(
                hierarchy.Outcome,
                hierarchy.Messages.Count > 0 ? hierarchy.Messages[0] : string.Empty,
                hierarchy.Errors.ToArray());
        }

        var user = await userRepository.GetByIdWithEmailsAsync(targetUserId, ct);
        if (user is null)
        {
            return Result<AdminResetEligibility>.Failure(
                UserErrors.NotFound,
                Outcome.NotFound);
        }

        var primary = user.GetPrimaryEmail();
        var primaryEmail = primary?.Address ?? string.Empty;
        var isVerified = primary?.IsVerified ?? false;

        return Result<AdminResetEligibility>.Success(new AdminResetEligibility(
            TargetUserId: user.Id,
            PrimaryEmail: primaryEmail,
            IsPrimaryEmailVerified: isVerified,
            Lifecycle: ToContractSnapshot(user.LifecycleState)));
    }

    public async Task<Result> EnsureCanManageUserAsync(
        Guid actorUserId,
        Guid targetUserId,
        CancellationToken cancellationToken = default)
    {
        _ = actorUserId;

        var guard = await roleHierarchy.EnsureCanManageUserAsync(targetUserId, cancellationToken);
        if (guard.IsFailure)
            return guard;

        var targetExists = await userRepository.AnyAsync(
            u => u.Id == targetUserId,
            cancellationToken);

        if (!targetExists)
        {
            return Result.Failure(UserErrors.NotFound, Outcome.NotFound);
        }

        return Result.Success();
    }

    public async Task<Result> SuspendUserByAdminAsync(
        Guid targetUserId,
        Guid actorUserId,
        CancellationToken ct = default)
    {
        _ = actorUserId;

        var guard = await roleHierarchy.EnsureCanManageUserAsync(targetUserId, ct);
        if (guard.IsFailure)
            return guard;

        var user = await userRepository.GetByIdAsync(targetUserId, ct, asNoTracking: false);
        if (user is null)
        {
            return Result.Failure(UserErrors.NotFound, Outcome.NotFound);
        }

        if (user.LifecycleState is AccountLifecycleState.Provisioned
            or AccountLifecycleState.PendingActivation
            or AccountLifecycleState.Archived)
        {
            return Result.Failure(
                Error.Conflict(
                    "User.IneligibleForSuspend",
                    $"Account is in state '{user.LifecycleState}' and cannot be suspended."),
                Outcome.Conflict);
        }

        if (user.LifecycleState == AccountLifecycleState.Suspended)
            return Result.Success();

        user.Suspend();
        await unitOfWork.SaveChangesAsync(ct);

        await cache.RemoveByTagAsync(SecurityCacheKeys.UserTag(targetUserId), ct);
        await cache.RemoveByTagAsync(SecurityCacheKeys.UsersTag, ct);

        return Result.Success();
    }

    public async Task<Result> ReactivateUserByAdminAsync(
        Guid targetUserId,
        Guid actorUserId,
        CancellationToken ct = default)
    {
        _ = actorUserId;

        var guard = await roleHierarchy.EnsureCanManageUserAsync(targetUserId, ct);
        if (guard.IsFailure)
            return guard;

        var user = await userRepository.GetByIdAsync(targetUserId, ct, asNoTracking: false);
        if (user is null)
        {
            return Result.Failure(UserErrors.NotFound, Outcome.NotFound);
        }

        if (user.LifecycleState != AccountLifecycleState.Suspended)
        {
            return Result.Failure(
                Error.Conflict(
                    "User.IneligibleForReactivate",
                    $"Account is in state '{user.LifecycleState}' and cannot be reactivated."),
                Outcome.Conflict);
        }

        user.Reactivate();
        await unitOfWork.SaveChangesAsync(ct);

        await cache.RemoveByTagAsync(SecurityCacheKeys.UserTag(targetUserId), ct);
        await cache.RemoveByTagAsync(SecurityCacheKeys.UsersTag, ct);

        return Result.Success();
    }

    public async Task<Result> ArchiveUserByAdminAsync(
        Guid targetUserId,
        Guid actorUserId,
        CancellationToken ct = default)
    {
        _ = actorUserId;

        var guard = await roleHierarchy.EnsureCanManageUserAsync(targetUserId, ct);
        if (guard.IsFailure)
            return guard;

        var user = await userRepository.GetByIdAsync(targetUserId, ct, asNoTracking: false);
        if (user is null)
        {
            return Result.Failure(UserErrors.NotFound, Outcome.NotFound);
        }

        if (user.LifecycleState == AccountLifecycleState.Archived)
        {
            return Result.Failure(
                Error.Conflict("User.AlreadyArchived", "Account is already archived."),
                Outcome.Conflict);
        }

        user.Archive();
        await unitOfWork.SaveChangesAsync(ct);

        await cache.RemoveByTagAsync(SecurityCacheKeys.UserTag(targetUserId), ct);
        await cache.RemoveByTagAsync(SecurityCacheKeys.UsersTag, ct);

        return Result.Success();
    }

    public async Task<Result<ReassignmentCompleted>> ReassignUserByAdminAsync(
        Guid targetUserId,
        Guid actorUserId,
        string newEmail,
        CancellationToken ct = default)
    {
        _ = actorUserId;

        if (string.IsNullOrWhiteSpace(newEmail))
        {
            return Result<ReassignmentCompleted>.Failure(
                Error.Validation("User.Email", "New email is required."),
                Outcome.Invalid);
        }

        var normalizedNewEmail = newEmail.Trim().ToLowerInvariant();

        var guard = await roleHierarchy.EnsureCanManageUserAsync(targetUserId, ct);
        if (guard.IsFailure)
        {
            return Result<ReassignmentCompleted>.Fail(
                guard.Outcome,
                guard.Messages.Count > 0 ? guard.Messages[0] : string.Empty,
                guard.Errors.ToArray());
        }

        // 2. Load target (tracked — we will mutate the aggregate).
        var user = await userRepository.GetByIdWithEmailsAsync(targetUserId, ct);
        if (user is null)
        {
            return Result<ReassignmentCompleted>.Failure(
                UserErrors.NotFound,
                Outcome.NotFound);
        }

        // 3. Lifecycle eligibility — reject Provisioned / PendingActivation
        //    / Archived. Active / Suspended / PendingPasswordReset proceed.
        if (user.LifecycleState is AccountLifecycleState.Provisioned
            or AccountLifecycleState.PendingActivation
            or AccountLifecycleState.Archived)
        {
            return Result<ReassignmentCompleted>.Failure(
                Error.Conflict(
                    "User.IneligibleForReassign",
                    $"Account is in state '{user.LifecycleState}' and cannot be reassigned."),
                Outcome.Conflict);
        }

        var primary = user.GetPrimaryEmail();
        if (primary is null)
        {
            return Result<ReassignmentCompleted>.Failure(
                Error.Conflict("User.NoPrimaryEmail", "Target account has no primary email."),
                Outcome.Conflict);
        }

        var oldEmail = primary.Address;
        if (!string.Equals(oldEmail, normalizedNewEmail, StringComparison.Ordinal))
        {
            var inUse = await userRepository.AnyAsync(
                u => u.Emails.Any(e => e.Address == normalizedNewEmail),
                ct);

            if (inUse)
            {
                return Result<ReassignmentCompleted>.Failure(
                    Error.Conflict("User.Email", "An account with this email already exists."),
                    Outcome.Conflict);
            }
        }
        var placeholderHash = "REASSIGNED:" + Guid.NewGuid().ToString("N");

        user.ReassignToPendingActivation(normalizedNewEmail, placeholderHash);

        await unitOfWork.SaveChangesAsync(ct);

        await cache.RemoveByTagAsync(SecurityCacheKeys.UserTag(targetUserId), ct);
        await cache.RemoveByTagAsync(SecurityCacheKeys.UsersTag, ct);

        return Result<ReassignmentCompleted>.Success(new ReassignmentCompleted(
            TargetUserId: targetUserId,
            OldEmail: oldEmail,
            NewEmail: normalizedNewEmail,
            Lifecycle: ToContractSnapshot(user.LifecycleState)));
    }
}
