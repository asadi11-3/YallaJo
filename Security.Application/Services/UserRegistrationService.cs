using Microsoft.Extensions.Caching.Hybrid;
using Security.Application.Caching;
using Security.Application.Helpers;
using Security.Application.Interfaces;
using Security.Contracts.Abstractions;
using Security.Domain.Entities;
using Security.Domain.Repositories;
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
    ISecurityUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
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

        // Invited user: no password, unverified email, inactive account.
        // These transitions happen only after the invitee accepts the invite
        // (proving mailbox ownership) via CompleteInviteAsync.
        var user = User.Register(normalizedEmail, request.FirstName, request.LastName);

        await userRepository.AddAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync(SecurityCacheKeys.UsersTag, cancellationToken);

        return Result<Guid>.Created(user.Id);
    }

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
}
