using ContentTours.Application.Caching;
using ContentTours.Application.Interfaces;
using ContentTours.Contracts.IntegrationEvents;
using ContentTours.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Commands.TourPackage.DeleteTourPackage;

/// <summary>
/// Soft-deletes a TourPackage via inherited <c>AuditableEntity.SoftDelete()</c>.
/// Per Decision #6 the DELETE endpoint does not require RowVersion in v1.
/// </summary>
public sealed class DeleteTourPackageCommandHandler(
    ITourPackageRepository repository,
    IContentToursUnitOfWork unitOfWork,
    IContentToursOutboxWriter outbox,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<DeleteTourPackageCommandHandler> logger)
    : ICommandHandler<DeleteTourPackageCommand>
{
    public async Task<Result> Handle(
        DeleteTourPackageCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (currentUser.UserId is null)
            {
                return Result.Failure(
                    new Error("Auth.Unauthorized", "Authentication is required."),
                    Outcome.Unauthorized);
            }

            var callerId = currentUser.UserId.Value;

            var package = await repository
                .GetByIdWithDetailsAsync(request.Id, cancellationToken)
                .ConfigureAwait(false);

            if (package is null)
            {
                return Result.Failure(
                    new Error("TourPackage.NotFound", $"Tour package '{request.Id}' was not found."),
                    Outcome.NotFound);
            }

            var isAdminTier = AppRoles.HighestPrivilegeLevel(currentUser.Roles)
                >= RolePrivilegeLevel.Admin;
            if (!isAdminTier && package.CreatedByUserId != callerId)
            {
                return Result.Failure(
                    new Error(
                        "TourPackage.NotOwner",
                        "You do not have permission to delete this tour package."),
                    Outcome.Forbidden);
            }

            // Capture the included-tour set BEFORE the mutation so we can both raise
            // a meaningful integration event and invalidate per-tour cache tags.
            var includedTourIds = package.IncludedTours.Select(x => x.TourId).ToList();

            package.SoftDelete();

            outbox.Enqueue(new TourPackageDeletedIntegrationEvent(
                PackageId:       package.Id,
                DeletedByUserId: callerId,
                IncludedTourIds: includedTourIds));

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex,
                    "Concurrency conflict deleting TourPackage {PackageId}", package.Id);
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
            foreach (var tourId in includedTourIds)
            {
                await cache.RemoveByTagAsync(ContentToursCacheKeys.TagForTour(tourId), cancellationToken)
                    .ConfigureAwait(false);
            }

            logger.LogInformation(
                "TourPackage {PackageId} soft-deleted by {UserId}", package.Id, callerId);

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
