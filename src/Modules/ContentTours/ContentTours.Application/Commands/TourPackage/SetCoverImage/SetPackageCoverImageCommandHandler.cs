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

namespace ContentTours.Application.Commands.TourPackage.SetCoverImage;

/// <summary>
/// Persists the cover image URL onto a tour package (mirrors the tour-guide avatar flow).
/// The package is loaded tracking, mutated via <c>SetCoverImage</c>, saved, cache is evicted,
/// and any previously stored local file is best-effort deleted.
/// </summary>
public sealed class SetPackageCoverImageCommandHandler(
    ITourPackageRepository packageRepository,
    IContentToursUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    IFileStorageService fileStorage,
    ILogger<SetPackageCoverImageCommandHandler> logger)
    : ICommandHandler<SetPackageCoverImageCommand>
{
    public async Task<Result> Handle(SetPackageCoverImageCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!currentUser.IsAuthenticated || currentUser.UserId is null)
                return Result.Failure(new Error("TourPackage.Unauthorized", "Authentication is required."), Outcome.Unauthorized);

            var callerId = currentUser.UserId.Value;

            // Load tracking (scalar mutation only — no eager includes required).
            var package = await packageRepository
                .GetByIdAsync(request.Id, cancellationToken, asNoTracking: false)
                .ConfigureAwait(false);

            if (package is null)
                return Result.Failure(
                    new Error("TourPackage.NotFound", $"Tour package '{request.Id}' was not found."),
                    Outcome.NotFound);

            // Owner gate (canonical project pattern; admins pass permission at endpoint layer).
            if (package.CreatedByUserId != callerId)
                return Result.Failure(
                    new Error("TourPackage.NotOwner", "You do not have permission to update this tour package."),
                    Outcome.Forbidden);

            var oldCoverUrl = package.CoverImageUrl;

            var setResult = package.SetCoverImage(request.CoverImageUrl);
            if (setResult.IsFailure)
            {
                var setError = setResult.Errors.FirstOrDefault()
                    ?? new Error("TourPackage.InvalidCoverImageUrl", "Unable to update the cover image.");
                return Result.Failure(setError, Outcome.UnprocessableEntity);
            }

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure(
                    new Error("TourPackage.ConcurrencyConflict", "Concurrent update detected."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(ContentToursCacheKeys.TagForPackage(package.Id), cancellationToken)
                .ConfigureAwait(false);
            await cache.RemoveByTagAsync(ContentToursCacheKeys.TagPackagesList, cancellationToken)
                .ConfigureAwait(false);

            await TryDeleteOldLocalCoverAsync(oldCoverUrl, request.CoverImageUrl, cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Cover image updated for TourPackage {PackageId}", package.Id);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure(new Error("Request.Cancelled", "The request was cancelled."), Outcome.Canceled);
        }
    }

    private async Task TryDeleteOldLocalCoverAsync(string? oldUrl, string? newUrl, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(oldUrl))
            return;

        // Only delete files we manage locally; never touch external URLs.
        if (!oldUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            return;

        // Do not delete the file we just persisted.
        if (string.Equals(oldUrl, newUrl, StringComparison.Ordinal))
            return;

        try
        {
            await fileStorage.DeleteAsync(oldUrl, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to delete old tour package cover file; an orphaned file can be cleaned up manually.");
        }
    }
}
