using Accounts.Application.Caching;
using Accounts.Domain.Errors;
using Accounts.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Application.Commands.RestoreProfile;

/// <summary>
/// Self-service handler for the inverse of <c>DeleteProfileCommand</c>:
/// restores the current user's previously soft-deleted profile so subsequent
/// reads via <c>GetProfileQuery</c> succeed again.
/// <para>
/// Loads the profile via <see cref="IProfileRepository.FirstOrDefaultIncludingDeletedAsync"/>
/// so the EF soft-delete query filter does not hide the row. Idempotent: if
/// the loaded profile already has <c>IsDeleted == false</c>, the underlying
/// <c>AuditableEntity.Restore()</c> early-returns and SaveChanges is still
/// called (no-op write) so cache invalidation runs uniformly. Returns
/// <see cref="ProfileErrors.NotFound"/> only when no profile row exists at
/// all for the caller's UserId.
/// </para>
/// </summary>
public sealed class RestoreProfileCommandHandler(
    IProfileRepository profileRepository,
    IAccountsUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    HybridCache cache)
    : ICommandHandler<RestoreProfileCommand>
{
    public async Task<Result> Handle(RestoreProfileCommand request, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            return Result.Failure(
                Error.Unauthorized("Authentication is required."),
                Outcome.Unauthorized);
        }

        var userId = currentUser.UserId.Value;

        var profile = await profileRepository.FirstOrDefaultIncludingDeletedAsync(
            filter: p => p.UserId == userId,
            asNoTracking: false,
            ct: ct);

        if (profile is null)
        {
            return Result.Failure(
                ProfileErrors.NotFound,
                Outcome.NotFound);
        }

        profile.Restore();

        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(
                new Error("Profile.ConcurrencyConflict", "The profile was modified concurrently. Reload and retry."),
                Outcome.Conflict);
        }

        await cache.RemoveByTagAsync(AccountsCacheKeys.UserProfileTag(userId), ct);

        return Result.Success();
    }
}
