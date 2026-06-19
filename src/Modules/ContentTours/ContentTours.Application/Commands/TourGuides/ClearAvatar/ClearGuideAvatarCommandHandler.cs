using System;
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

namespace ContentTours.Application.Commands.TourGuides.ClearAvatar;

public sealed class ClearGuideAvatarCommandHandler(
    ITourGuideRepository guideRepository,
    IContentToursUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    IFileStorageService fileStorage,
    ILogger<ClearGuideAvatarCommandHandler> logger)
    : ICommandHandler<ClearGuideAvatarCommand>
{
    public async Task<Result> Handle(ClearGuideAvatarCommand request, CancellationToken cancellationToken)
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

            guide.ClearAvatar();

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

            await TryDeleteOldLocalAvatarAsync(oldAvatarUrl, cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Tour guide avatar cleared for profile {ProfileId}", guide.Id);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure(new Error("Request.Cancelled", "The request was cancelled."), Outcome.Canceled);
        }
    }

    private async Task TryDeleteOldLocalAvatarAsync(string? oldAvatarUrl, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(oldAvatarUrl))
            return;

        // Only delete files we manage locally; never touch external URLs.
        if (!oldAvatarUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
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
