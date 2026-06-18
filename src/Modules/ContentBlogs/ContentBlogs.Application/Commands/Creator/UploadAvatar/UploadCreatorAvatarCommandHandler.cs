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

namespace ContentBlogs.Application.Commands.Creator.UploadAvatar;

/// <summary>
/// Persists a validated, already-uploaded creator avatar URL onto the current creator profile.
/// After a successful save it best-effort deletes the previously stored local avatar file
/// (only local <c>/uploads/</c> paths that differ from the new one); external URLs are never deleted
/// and a delete failure never fails the request.
/// </summary>
public sealed class UploadCreatorAvatarCommandHandler(
    ICreatorProfileRepository profileRepository,
    IContentBlogsUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    IFileStorageService fileStorage,
    ILogger<UploadCreatorAvatarCommandHandler> logger)
    : ICommandHandler<UploadCreatorAvatarCommand>
{
    public async Task<Result> Handle(
        UploadCreatorAvatarCommand request,
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

            profile.UpdateAvatar(request.AvatarUrl);
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

            await TryDeleteOldLocalAvatarAsync(oldAvatarUrl, request.AvatarUrl, cancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation(
                "Creator avatar uploaded for profile {ProfileId}", profile.Id);

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
    /// Best-effort deletion of the previous avatar blob. Only local <c>/uploads/</c> paths that
    /// differ from the new URL are removed; external URLs are left untouched and any failure is
    /// logged without failing the request.
    /// </summary>
    private async Task TryDeleteOldLocalAvatarAsync(
        string? oldAvatarUrl,
        string newAvatarUrl,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(oldAvatarUrl))
            return;

        if (!oldAvatarUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            return;

        if (string.Equals(oldAvatarUrl, newAvatarUrl, StringComparison.Ordinal))
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
