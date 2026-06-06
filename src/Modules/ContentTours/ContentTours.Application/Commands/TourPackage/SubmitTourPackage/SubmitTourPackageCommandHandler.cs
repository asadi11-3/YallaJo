using ContentTours.Application.Caching;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Commands.TourPackage.SubmitTourPackage;

/// <summary>
/// Submits a TourPackage for admin review (Draft|Rejected -> Submitted).
/// Caller MUST be the package creator.
/// </summary>
public sealed class SubmitTourPackageCommandHandler(
    ITourPackageRepository repository,
    IContentToursUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<SubmitTourPackageCommandHandler> logger)
    : ICommandHandler<SubmitTourPackageCommand>
{
    public async Task<Result> Handle(
        SubmitTourPackageCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var callerId = currentUser.UserId!.Value;

            var package = await repository
                .GetByIdWithDetailsAsync(request.PackageId, cancellationToken)
                .ConfigureAwait(false);

            if (package is null)
            {
                return Result.Failure(
                    new Error("TourPackage.NotFound", $"Tour package '{request.PackageId}' was not found."),
                    Outcome.NotFound);
            }

            if (package.CreatedByUserId != callerId)
            {
                return Result.Failure(
                    new Error(
                        "TourPackage.NotOwner",
                        "You do not have permission to submit this tour package."),
                    Outcome.Forbidden);
            }

            var submitResult = package.Submit(DateTime.UtcNow);
            if (submitResult.IsFailure)
            {
                return submitResult;
            }

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex,
                    "Concurrency conflict submitting TourPackage {PackageId}", package.Id);
                return Result.Failure(
                    new Error(
                        "TourPackage.ConcurrencyConflict",
                        "The package was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(ContentToursCacheKeys.TagForPackage(package.Id), cancellationToken)
                .ConfigureAwait(false);
            await cache.RemoveByTagAsync(ContentToursCacheKeys.TagPackagesList, cancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation(
                "TourPackage {PackageId} submitted for review by {UserId}", package.Id, callerId);

            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
