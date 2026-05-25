using Microsoft.Extensions.Caching.Hybrid;
using Security.Application.Caching;
using Security.Application.Helpers;
using Security.Application.Interfaces;
using Security.Contracts.Abstractions;
using Security.Contracts.Authorization;
using Security.Domain.Entities;
using Security.Domain.Errors;
using Security.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Application.Services;

internal sealed class UserRegistrationService(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    ISecurityUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    ICurrentUser currentUser,
    HybridCache cache)
    : IUserRegistrationService
{
    public async Task<Result<Guid>> RegisterAsync(
        UserRegistrationRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = SecurityGuard.NormalizeEmail(request.Email);

        if (await EmailExistsAsync(normalizedEmail, cancellationToken))
        {
            return Result<Guid>.Conflict(
                Error.Conflict("User.Email", "An account with this email already exists."));
        }

        var user = User.Register(normalizedEmail, request.FirstName, request.LastName);
        user.SetInitialPasswordHash(passwordHasher.Hash(request.Password));

        // Assign default Guest role — upgraded to User on email verification
        var defaultRoles = await roleRepository.GetRolesByNamesAsync(AppRoles.DefaultRoles, cancellationToken);
        foreach (var defaultRole in defaultRoles)
        {
            user.AssignRole(defaultRole);
        }

        await userRepository.AddAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync(SecurityCacheKeys.UsersTag, cancellationToken);

        return Result<Guid>.Created(user.Id);
    }

    public async Task<Result<Guid>> RegisterProvisionedAsync(
        InvitedUserRegistrationRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = SecurityGuard.NormalizeEmail(request.Email);

        if (await EmailExistsAsync(normalizedEmail, cancellationToken))
        {
            return Result<Guid>.Conflict(
                Error.Conflict("User.Email", "An account with this email already exists."));
        }

        var roleIds = request.InitialRoleIds
            .Where(id => id != Guid.Empty)
            .Distinct().ToList();

        if (roleIds.Count == 0)
        {
            return Result<Guid>.Failure(
                Error.Validation("Invite.RoleIds", "At least one initial role must be selected."),
                Outcome.Invalid);
        }

        var assignable = await GetInvitableRolesInternalAsync(cancellationToken);
        if (assignable.IsFailure)
        {
            return Result<Guid>.Fail(
                assignable.Outcome,
                assignable.Messages.FirstOrDefault() ?? "Failed to resolve invitable roles.",
                assignable.Errors.ToArray());
        }

        var assignableIds = assignable.Value.Select(r => r.RoleId).ToHashSet();
        var disallowedIds = roleIds.Where(id => !assignableIds.Contains(id)).ToArray();
        if (disallowedIds.Length > 0)
        {
            return Result<Guid>.Failure(
                Error.Forbidden("You are not allowed to assign one or more selected roles."),
                Outcome.Forbidden);
        }
        var user = User.Register(normalizedEmail, request.FirstName, request.LastName);

        foreach (var roleId in roleIds)
        {
            var role = await roleRepository.GetByIdAsync(roleId, cancellationToken);
            if (role is null)
                return Result<Guid>.Failure(RoleErrors.NotFound, Outcome.NotFound);

            if (!role.IsActive)
                return Result<Guid>.Failure(RoleErrors.Inactive, Outcome.Invalid);

            if (role.Name == AppRoles.Owner
                && await userRepository.AnyWithRoleAsync(AppRoles.Owner, cancellationToken))
                return Result<Guid>.Failure(RoleErrors.OwnerSingleton, Outcome.Conflict);

            user.AssignRole(role);
        }

        await userRepository.AddAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cache.RemoveByTagAsync(SecurityCacheKeys.UsersTag, cancellationToken);

        return Result<Guid>.Created(user.Id);
    }

    public async Task<Result> MarkPendingActivationAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken, asNoTracking: false);
        if (user is null)
        {
            return Result.Fail(Outcome.NotFound, "No account found.", UserErrors.NotFound);
        }

        if (user.LifecycleState != AccountLifecycleState.Provisioned
         && user.LifecycleState != AccountLifecycleState.PendingActivation)
        {
            return Result.Failure(
                Error.Conflict(
                    "Invite.AlreadyCompleted",
                    "This account is past the activation window and cannot receive a new activation email."),
                Outcome.Conflict);
        }

        if (user.LifecycleState == AccountLifecycleState.PendingActivation)
        {
            return Result.Success();
        }

        user.MarkPendingActivation();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync(SecurityCacheKeys.UserTag(userId), cancellationToken);
        await cache.RemoveByTagAsync(SecurityCacheKeys.UsersTag, cancellationToken);

        return Result.Success();
    }

    public async Task<Result> MarkPendingPasswordResetAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken, asNoTracking: false);
        if (user is null)
        {
            return Result.Fail(Outcome.NotFound, "No account found.", UserErrors.NotFound);
        }

        if (user.LifecycleState != AccountLifecycleState.Active
         && user.LifecycleState != AccountLifecycleState.PendingPasswordReset)
        {
            return Result.Failure(
                Error.Conflict(
                    "User.IneligibleForPasswordReset",
                    $"Account is in state '{user.LifecycleState}' and is not eligible for admin-initiated password reset."),
                Outcome.Conflict);
        }

        if (user.LifecycleState == AccountLifecycleState.PendingPasswordReset)
        {
            return Result.Success();
        }

        user.MarkPendingPasswordReset();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync(SecurityCacheKeys.UserTag(userId), cancellationToken);
        await cache.RemoveByTagAsync(SecurityCacheKeys.UsersTag, cancellationToken);

        return Result.Success();
    }

    public async Task<Result<Guid>> RegisterExternalAsync(
        ExternalUserRegistrationRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = SecurityGuard.NormalizeEmail(request.Email);

        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return Result<Guid>.Failure(
                Error.Validation("User.Email", "Email is required."),
                Outcome.Invalid);
        }

        if (await EmailExistsAsync(normalizedEmail, cancellationToken))
        {
            return Result<Guid>.Conflict(
                Error.Conflict("User.Email", "An account with this email already exists."));
        }

        var firstName = string.IsNullOrWhiteSpace(request.FirstName) ? "User" : request.FirstName.Trim();
        var lastName  = string.IsNullOrWhiteSpace(request.LastName)  ? string.Empty : request.LastName.Trim();

        var user = User.Register(normalizedEmail, firstName, lastName);

        var primaryEmail = user.GetPrimaryEmail();
        if (primaryEmail is null)
        {
            return Result<Guid>.Failure(
                Error.Failure("User.NoPrimaryEmail", "Could not create primary email."),
                Outcome.ServerError);
        }

        user.VerifyEmail(primaryEmail.Id);

        user.SetInitialPasswordHash("EXTERNAL-ONLY:" + Guid.NewGuid().ToString("N"));

        // External (OAuth) users have verified email — assign User role directly
        var userRoles = await roleRepository.GetRolesByNamesAsync([AppRoles.User], cancellationToken);
        var userRoleEntity = userRoles.FirstOrDefault();
        if (userRoleEntity is not null)
        {
            user.AssignRole(userRoleEntity);
        }

        await userRepository.AddAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync(SecurityCacheKeys.UsersTag, cancellationToken);

        return Result<Guid>.Created(user.Id);
    }

    public Task<Result<IReadOnlyList<InvitableRoleOption>>> ListInvitableRolesAsync(
        CancellationToken cancellationToken = default) =>
        GetInvitableRolesInternalAsync(cancellationToken);

    public async Task<InviteAccountStatus?> GetInviteAccountStatusAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = SecurityGuard.NormalizeEmail(email);

        var userId = await userRepository.GetUserIdByEmailAsync(normalizedEmail, cancellationToken);
        if (userId is null)
            return null;

        var user = await userRepository.GetByIdWithEmailsAsync(userId.Value, cancellationToken);
        if (user is null)
            return null;

        var primary = user.GetPrimaryEmail();

        return new InviteAccountStatus(
            UserId:          user.Id,
            Email:           primary?.Address ?? normalizedEmail,
            IsEmailVerified: primary?.IsVerified ?? false,
            IsActive:        user.IsActive,
            Lifecycle:       ToContractSnapshot(user.LifecycleState));
    }

    public async Task<Result> CompleteActivationAsync(
        Guid userId,
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = SecurityGuard.NormalizeEmail(email);

        var user = await userRepository.GetByIdWithEmailsAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result.Fail(Outcome.NotFound, "No account found for this invite.", UserErrors.NotFound);
        }

        var primary = user.GetPrimaryEmail();
        if (primary is null || !string.Equals(primary.Address, normalizedEmail, StringComparison.Ordinal))
        {
            return Result.Failure(
                Error.Validation("Invite.EmailMismatch", "Invite email does not match the account."),
                Outcome.Invalid);
        }

        if (user.IsActive || primary.IsVerified)
        {
            return Result.Failure(
                Error.Conflict("Invite.AlreadyCompleted", "This account has already completed onboarding."),
                Outcome.Conflict);
        }

        user.SetPasswordHash(passwordHasher.Hash(password));
        user.VerifyEmail(primary.Id);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync(SecurityCacheKeys.UserTag(userId), cancellationToken);
        await cache.RemoveByTagAsync(SecurityCacheKeys.UsersTag, cancellationToken);

        return Result.Success();
    }

    private static AccountLifecycleSnapshot ToContractSnapshot(AccountLifecycleState state)
        => (AccountLifecycleSnapshot)(int)state;

    private Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken ct) =>
        userRepository.AnyAsync(
            u => u.Emails.Any(e => e.Address == normalizedEmail),
            ct);

    private async Task<Result<IReadOnlyList<InvitableRoleOption>>> GetInvitableRolesInternalAsync(
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
        {
            return Result<IReadOnlyList<InvitableRoleOption>>.Failure(
                Error.Unauthorized(),
                Outcome.Unauthorized);
        }

        var roles = await roleRepository.GetAllAsync(
            filter: r => r.IsActive,
            asNoTracking: true,
            ct: ct);

        var canAssignOwnerOnly = currentUser.IsInRole(AppRoles.Owner);

        var options = roles
            .Where(r => canAssignOwnerOnly || !AppRoles.OwnerOnlyRoles.Contains(r.Name, StringComparer.Ordinal))
            .Select(r => new InvitableRoleOption(
                RoleId: r.Id,
                Name: r.Name,
                Description: r.Description,
                IsPrivileged: AppRoles.ProtectedRoles.Contains(r.Name, StringComparer.Ordinal)))
            .OrderBy(r => r.Name, StringComparer.Ordinal)
            .ToList();

        return Result<IReadOnlyList<InvitableRoleOption>>.Success(options);
    }
}
