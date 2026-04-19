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

/// <summary>
/// Implementation of <see cref="IUserRegistrationService"/> — keeps the
/// <c>User</c> aggregate, password hashing, persistence, cache invalidation,
/// and the invite-finalization state transition entirely inside Security.
/// Exposed to other modules only through the contract.
/// </summary>
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
        user.SetPasswordHash(passwordHasher.Hash(request.Password));

        await userRepository.AddAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync(SecurityCacheKeys.UsersTag, cancellationToken);

        return Result<Guid>.Created(user.Id);
    }

    public async Task<Result<Guid>> RegisterInvitedAsync(
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
            .Distinct()
            .ToList();

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

        // Invited user: no password, unverified email, inactive account.
        // Pre-assigned roles are persisted now; the invite acceptance later
        // completes password + verification + activation only.
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
            UserId: user.Id,
            Email: primary?.Address ?? normalizedEmail,
            IsEmailVerified: primary?.IsVerified ?? false,
            IsActive: user.IsActive);
    }

    public async Task<Result> CompleteInviteAsync(
        Guid userId,
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = SecurityGuard.NormalizeEmail(email);

        var user = await userRepository.GetByIdWithEmailsAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(
                Error.NotFound("User.NotFound", "No account found for this invite."),
                Outcome.NotFound);
        }

        var primary = user.GetPrimaryEmail();
        if (primary is null || !string.Equals(primary.Address, normalizedEmail, StringComparison.Ordinal))
        {
            return Result.Failure(
                Error.Validation("Invite.EmailMismatch", "Invite email does not match the account."),
                Outcome.Invalid);
        }

        // Already onboarded (or partially finalized) → refuse re-acceptance.
        if (user.IsActive || primary.IsVerified)
        {
            return Result.Failure(
                Error.Conflict("Invite.AlreadyCompleted", "This account has already completed onboarding."),
                Outcome.Conflict);
        }

        user.SetPasswordHash(passwordHasher.Hash(password));
        user.VerifyEmail(primary.Id); // also activates the account via domain invariant

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync(SecurityCacheKeys.UsersTag, cancellationToken);

        return Result.Success();
    }

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
