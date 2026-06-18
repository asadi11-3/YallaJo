using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Interfaces;
using ContentBlogs.Domain.Errors;
using ContentBlogs.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Storage;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Commands.Creator.ClearAvatar;

/// <summary>
/// Clears the current creator's avatar URL. After a successful save it best-effort deletes the
/// previously stored local avatar file (only local <c>/uploads/</c> paths); external URLs are
/// never deleted and a delete failure never fails the request.
/// </summary>
public sealed class ClearCreatorAvatarCommandHandler(
    ICreatorProfileRepository profileRepository,
    IContentBlogsUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    IFileStorageService fileStorage,
    ILogger<ClearCreatorAvatarCommandHandler> logger)
    : ICommandHandler<ClearCreatorAvatarCommand>
{
    public async Task<Result> Handle(
        ClearCreatorAvatarCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var profile = await profileRepository
                .GetByUserIdAsync(currentUser.UserId!.Value, cancellationToken)
                .ConfigureAwait(false);

            if (profile is null)
                return Result.Failure(CreatorProfileErrors.NotFound, Outcome.NotFound);

            var oldAvatarUrl = profile.AvatarUrl;

            profile.ClearAvatar();
            profileRepository.Update(profile);
            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure(
                    new Error("Creator.ConcurrencyConflict", "Profile was modified concurrently."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(
                ContentBlogsCacheKeys.CreatorProfileTag(profile.Id), cancellationToken)
                .ConfigureAwait(false);
            await cache.RemoveByTagAsync(
                ContentBlogsCacheKeys.CreatorProfileUserTag(profile.UserId), cancellationToken)
                .ConfigureAwait(false);

            await TryDeleteOldLocalAvatarAsync(oldAvatarUrl, cancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation(
                "Creator avatar cleared for profile {ProfileId}", profile.Id);

            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }

    /// <summary>
    /// Best-effort deletion of the previous avatar blob. Only local <c>/uploads/</c> paths are
    /// removed; external URLs are left untouched and any failure is logged without failing the request.
    /// </summary>
    private async Task TryDeleteOldLocalAvatarAsync(
        string? oldAvatarUrl,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(oldAvatarUrl))
            return;

        if (!oldAvatarUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            return;

        try
        {
            await fileStorage.DeleteAsync(oldAvatarUrl, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Failed to delete old creator avatar file; an orphaned file can be cleaned up manually.");
        }
    }
}
