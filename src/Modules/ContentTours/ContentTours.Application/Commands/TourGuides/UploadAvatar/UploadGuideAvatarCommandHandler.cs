using System;
using System.Linq;
using ContentTours.Application.Caching;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Storage;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Commands.TourGuides.UploadAvatar;

public sealed class UploadGuideAvatarCommandHandler(
    ITourGuideRepository guideRepository,
    IContentToursUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    IFileStorageService fileStorage,
    ILogger<UploadGuideAvatarCommandHandler> logger)
    : ICommandHandler<UploadGuideAvatarCommand>
{
    public async Task<Result> Handle(UploadGuideAvatarCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!currentUser.IsAuthenticated || currentUser.UserId is null)
                return Result.Failure(new Error("TourGuide.Unauthorized", "Authentication is required."), Outcome.Unauthorized);

            var guide = await guideRepository
                .GetByUserIdAsync(currentUser.UserId.Value, cancellationToken, asNoTracking: false)
                .ConfigureAwait(false);
            if (guide is null)
                return Result.Failure(new Error("TourGuide.NotFound", "Tour guide profile not found."), Outcome.NotFound);

            var oldAvatarUrl = guide.AvatarUrl;

            var updateResult = guide.UpdateAvatar(request.AvatarUrl);
            if (updateResult.IsFailure)
            {
                var updateError = updateResult.Errors.FirstOrDefault()
                    ?? new Error("TourGuide.InvalidAvatarUrl", "Unable to update the avatar.");
                return Result.Failure(updateError, Outcome.UnprocessableEntity);
            }

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure(new Error("TourGuide.ConcurrencyConflict", "Concurrent update detected."), Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(TourGuideCacheKeys.TagForProfile(guide.Id), cancellationToken).ConfigureAwait(false);
            await cache.RemoveByTagAsync(TourGuideCacheKeys.TagForProfileByUser(guide.UserId), cancellationToken).ConfigureAwait(false);

            await TryDeleteOldLocalAvatarAsync(oldAvatarUrl, request.AvatarUrl, cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Tour guide avatar uploaded for profile {ProfileId}", guide.Id);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure(new Error("Request.Cancelled", "The request was cancelled."), Outcome.Canceled);
        }
    }

    private async Task TryDeleteOldLocalAvatarAsync(string? oldAvatarUrl, string? newAvatarUrl, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(oldAvatarUrl))
            return;

        // Only delete files we manage locally; never touch external URLs.
        if (!oldAvatarUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            return;

        // Do not delete the file we just persisted.
        if (string.Equals(oldAvatarUrl, newAvatarUrl, StringComparison.Ordinal))
            return;

        try
        {
            await fileStorage.DeleteAsync(oldAvatarUrl, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to delete old tour guide avatar file; an orphaned file can be cleaned up manually.");
        }
    }
}
