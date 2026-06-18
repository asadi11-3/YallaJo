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

namespace Accounts.Application.Commands.DeleteAvatar;

public sealed class DeleteAvatarCommandHandler(
    IProfileRepository profileRepository,
    IAccountsUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    HybridCache cache,
    IFileStorageService fileStorage,
    ILogger<DeleteAvatarCommandHandler> logger)
    : ICommandHandler<DeleteAvatarCommand, DeleteAvatarResult>
{
    public async Task<Result<DeleteAvatarResult>> Handle(
        DeleteAvatarCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            return Result<DeleteAvatarResult>.Failure(
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
            return Result<DeleteAvatarResult>.Failure(
                ProfileErrors.NotFound,
                Outcome.NotFound);
        }

        var oldAvatarUrl = profile.AvatarUrl;

        profile.DeleteAvatar();

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<DeleteAvatarResult>.Failure(
                new Error("Profile.ConcurrencyConflict", "The profile was modified concurrently. Reload and retry."),
                Outcome.Conflict);
        }

        await cache.RemoveByTagAsync(AccountsCacheKeys.UserProfileTag(userId), cancellationToken);

        // Best-effort cleanup of the removed local avatar blob. Only delete files we own
        // (rooted under /uploads/). External (OAuth) avatar URLs are left untouched.
        // Failures here must never fail the request.
        await TryDeleteOldLocalAvatarAsync(oldAvatarUrl, cancellationToken);

        return Result<DeleteAvatarResult>.Success(new DeleteAvatarResult(true));
    }

    private async Task TryDeleteOldLocalAvatarAsync(
        string? oldAvatarUrl,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(oldAvatarUrl) ||
            !oldAvatarUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
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
                "Failed to delete previous avatar blob during avatar deletion. The orphaned file can be cleaned up manually.");
        }
    }
}
