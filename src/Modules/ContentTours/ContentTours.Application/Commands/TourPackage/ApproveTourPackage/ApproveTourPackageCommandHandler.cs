using ContentTours.Application.Caching;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Commands.TourPackage.ApproveTourPackage;

/// <summary>
/// Admin approves a Submitted TourPackage. Transition: Submitted -> Approved.
/// Admin authorization MUST be enforced at the route level.
/// </summary>
public sealed class ApproveTourPackageCommandHandler(
    ITourPackageRepository repository,
    IContentToursUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<ApproveTourPackageCommandHandler> logger)
    : ICommandHandler<ApproveTourPackageCommand>
{
    public async Task<Result> Handle(
        ApproveTourPackageCommand request,
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

            var approveResult = package.Approve(adminId, DateTime.UtcNow);
            if (approveResult.IsFailure)
            {
                return approveResult;
            }

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex,
                    "Concurrency conflict approving TourPackage {PackageId}", package.Id);
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
                "TourPackage {PackageId} approved by admin {AdminId}", package.Id, adminId);

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
