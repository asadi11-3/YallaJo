using ContentTours.Application.Caching;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Commands.TourPackage.RejectTourPackage;

/// <summary>
/// Admin rejects a Submitted TourPackage with a reason. Transition: Submitted -> Rejected.
/// Admin authorization MUST be enforced at the route level.
/// </summary>
public sealed class RejectTourPackageCommandHandler(
    ITourPackageRepository repository,
    IContentToursUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<RejectTourPackageCommandHandler> logger)
    : ICommandHandler<RejectTourPackageCommand>
{
    public async Task<Result> Handle(
        RejectTourPackageCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var adminId = currentUser.UserId!.Value;

            var package = await repository
                .GetByIdWithDetailsAsync(request.PackageId, cancellationToken)
                .ConfigureAwait(false);

            if (package is null)
            {
                return Result.Failure(
                    new Error("TourPackage.NotFound", $"Tour package '{request.PackageId}' was not found."),
                    Outcome.NotFound);
            }

            var rejectResult = package.Reject(adminId, request.Reason, DateTime.UtcNow);
            if (rejectResult.IsFailure)
            {
                return rejectResult;
            }

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex,
                    "Concurrency conflict rejecting TourPackage {PackageId}", package.Id);
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
                "TourPackage {PackageId} rejected by admin {AdminId}", package.Id, adminId);

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
