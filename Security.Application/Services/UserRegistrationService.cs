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
        user.SetInitialPasswordHash(passwordHasher.Hash(request.Password));

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

        // Phase 2B — pure provisioning. The identity is created in
        // Provisioned state with no password, unverified email, and NO
        // lifecycle advance. The Provisioned -> PendingActivation transition
        // is the responsibility of MarkPendingActivationAsync, invoked by
        // the SendActivationEmail pipeline after email delivery succeeds.
        // This keeps the on-record lifecycle honest: an account never sits
        // in PendingActivation unless an activation email was actually sent.
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
            return Result.Failure(
                Error.NotFound("User.NotFound", "No account found."),
                Outcome.NotFound);
        }

        // Gate: only Provisioned (first send) or PendingActivation (resend)
        // may transition here. Any later state indicates the account is
        // already past the activation window and must be rejected so an
        // activation email can't silently reset an active/suspended/
        // archived account.
        if (user.LifecycleState != AccountLifecycleState.Provisioned
         && user.LifecycleState != AccountLifecycleState.PendingActivation)
        {
            return Result.Failure(
                Error.Conflict(
                    "Invite.AlreadyCompleted",
                    "This account is past the activation window and cannot receive a new activation email."),
                Outcome.Conflict);
        }

        // Idempotent: domain MarkPendingActivation is a no-op self-transition
        // when already in PendingActivation. We still call SaveChanges only
        // when a real transition occurs, to avoid UoW churn on no-ops.
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

    [Obsolete("Use RegisterProvisionedAsync (then MarkPendingActivationAsync once the activation email is dispatched). Kept for Phase 2A+2B backward compatibility; will be removed in Phase 4.")]
    public async Task<Result<Guid>> RegisterInvitedAsync(
        InvitedUserRegistrationRequest request,
        CancellationToken cancellationToken = default)
    {
        // Phase 2B compatibility shim — provision + immediate pending-activation
        // transition in a single call, matching the pre-split observable
        // behavior. Callers that need the honest "email-sent ⇒ pending-
        // activation" semantics should use RegisterProvisionedAsync +
        // MarkPendingActivationAsync via the SendActivationEmailCommand.
        var provisioned = await RegisterProvisionedAsync(request, cancellationToken);
        if (provisioned.IsFailure)
            return provisioned;

        var transition = await MarkPendingActivationAsync(provisioned.Value, cancellationToken);
        if (transition.IsFailure)
        {
            // Should not happen — the account was just created in Provisioned.
            // Propagate the failure faithfully rather than masking it.
            return Result<Guid>.Fail(
                transition.Outcome,
                transition.Messages.FirstOrDefault() ?? "Failed to mark account pending activation.",
                transition.Errors.ToArray());
        }

        return Result<Guid>.Created(provisioned.Value);
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

        // Refuse to silently merge into an existing local account — the caller
        // (ExternalLoginCommandHandler) is responsible for auto-linking when a
        // local account exists. Auto-create only fires for TRULY new users.
        if (await EmailExistsAsync(normalizedEmail, cancellationToken))
        {
            return Result<Guid>.Conflict(
                Error.Conflict("User.Email", "An account with this email already exists."));
        }

        // Create the User aggregate and mark the primary email verified BEFORE
        // persistence — VerifyEmail() also flips IsActive=true via the domain
        // invariant, so the user is fully onboarded in one transaction.
        var firstName = string.IsNullOrWhiteSpace(request.FirstName) ? "User" : request.FirstName.Trim();
        var lastName  = string.IsNullOrWhiteSpace(request.LastName)  ? string.Empty : request.LastName.Trim();

        var user = User.Register(normalizedEmail, firstName, lastName);

        var primaryEmail = user.GetPrimaryEmail();
        if (primaryEmail is null)
        {
            // Defensive — User.Register always adds a primary email; tripping
            // this means the aggregate contract changed.
            return Result<Guid>.Failure(
                Error.Failure("User.NoPrimaryEmail", "Could not create primary email."),
                Outcome.ServerError);
        }

        user.VerifyEmail(primaryEmail.Id);

        // NO password is set — the `PasswordHash` column is NOT NULL in the
        // schema, so we seed an inert, non-usable placeholder that no
        // plaintext can ever match (the hash format is unrecognizable to
        // PasswordHasher.Verify, guaranteeing any password-login attempt
        // fails closed). The user sets a real password later via
        // forgot-password → reset, which replaces this placeholder.
        user.SetInitialPasswordHash("EXTERNAL-ONLY:" + Guid.NewGuid().ToString("N"));

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

        // Phase 2B — explicit activation:
        //   1. Set the initial password.
        //   2. Mark the primary email verified. User.VerifyEmail internally
        //      routes through TransitionTo(Active), which in this context
        //      represents the explicit PendingActivation -> Active
        //      transition (PendingActivation is a legal predecessor for
        //      Active in IsTransitionAllowed). The lifecycle transition
        //      event fires alongside the EmailVerifiedEvent, giving
        //      downstream audit/telemetry a single deterministic record of
        //      the activation.
        user.SetPasswordHash(passwordHasher.Hash(password));
        user.VerifyEmail(primary.Id);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync(SecurityCacheKeys.UserTag(userId), cancellationToken);
        await cache.RemoveByTagAsync(SecurityCacheKeys.UsersTag, cancellationToken);

        return Result.Success();
    }

    [Obsolete("Use CompleteActivationAsync. Kept for Phase 2A+2B backward compatibility; will be removed in Phase 4.")]
    public Task<Result> CompleteInviteAsync(
        Guid userId,
        string email,
        string password,
        CancellationToken cancellationToken = default)
        => CompleteActivationAsync(userId, email, password, cancellationToken);

    /// <summary>
    /// Maps the domain <see cref="AccountLifecycleState"/> enum to its
    /// cross-module contract counterpart. Ordinals are guaranteed aligned
    /// by Phase 2A discipline — kept as a helper method so any future
    /// divergence has a single point of failure.
    /// </summary>
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
