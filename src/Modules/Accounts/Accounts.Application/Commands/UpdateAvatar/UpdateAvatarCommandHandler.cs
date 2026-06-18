using Accounts.Application.Caching;
using Accounts.Domain.Errors;
using Accounts.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Storage;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Application.Commands.UpdateAvatar;

public sealed class UpdateAvatarCommandHandler(
    IProfileRepository profileRepository,
    IAccountsUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    HybridCache cache,
    IFileStorageService fileStorage,
    ILogger<UpdateAvatarCommandHandler> logger)
    : ICommandHandler<UpdateAvatarCommand, UpdateAvatarResult>
{
    public async Task<Result<UpdateAvatarResult>> Handle(
        UpdateAvatarCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            return Result<UpdateAvatarResult>.Failure(
                Error.Unauthorized("Authentication is required."),
                Outcome.Unauthorized);
        }

        var userId = currentUser.UserId.Value;

        var profile = await profileRepository.FirstOrDefaultAsync(
            filter: p => p.UserId == userId,
            asNoTracking: false,
            ct: cancellationToken);

        if (profile is null)
        {
            return Result<UpdateAvatarResult>.Failure(
                ProfileErrors.NotFound,
                Outcome.NotFound);
        }

        var oldAvatarUrl = profile.AvatarUrl;

        profile.UpdateAvatar(request.AvatarUrl);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<UpdateAvatarResult>.Failure(
                new Error("Profile.ConcurrencyConflict", "The profile was modified concurrently. Reload and retry."),
                Outcome.Conflict);
        }

        await cache.RemoveByTagAsync(AccountsCacheKeys.UserProfileTag(userId), cancellationToken);

        // Best-effort cleanup of the previous local avatar blob. Only delete files we own
        // (rooted under /uploads/) and never the just-saved one. External (OAuth) avatar
        // URLs are left untouched. Failures here must never fail the request.
        await TryDeleteOldLocalAvatarAsync(oldAvatarUrl, request.AvatarUrl, cancellationToken);

        return Result<UpdateAvatarResult>.Success(new UpdateAvatarResult(profile.AvatarUrl!));
    }

    private async Task TryDeleteOldLocalAvatarAsync(
        string? oldAvatarUrl,
        string? newAvatarUrl,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(oldAvatarUrl) ||
            !oldAvatarUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(oldAvatarUrl, newAvatarUrl, StringComparison.Ordinal))
        {
            return;
        }

        try
        {
            await fileStorage.DeleteAsync(oldAvatarUrl, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Failed to delete previous avatar blob during avatar update. The orphaned file can be cleaned up manually.");
        }
    }
}
