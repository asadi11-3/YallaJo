using ContentTours.Application.Caching;
using ContentTours.Application.Interfaces;
using ContentTours.Contracts;
using ContentTours.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Commands.Tour.DeleteTour;

public sealed class DeleteTourCommandHandler(
    ITourRepository tourRepository,
    IContentToursOutboxWriter outboxWriter,
    IContentToursUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<DeleteTourCommandHandler> logger)
    : ICommandHandler<DeleteTourCommand>
{
    public async Task<Result> Handle(DeleteTourCommand request, CancellationToken cancellationToken)
    {
        try
        {
            

            var tour = await tourRepository
                .GetByIdAsync(request.Id, cancellationToken, asNoTracking: false)
                .ConfigureAwait(false);
            if (tour is null)
            {
                return Result.Failure(
                    new Error("Tour.NotFound", $"Tour '{request.Id}' was not found."),
                    Outcome.NotFound);
            }

            // P1 DeleteOwn/DeleteAny (2026-05-30): owner OR admin-tier may delete.
            // DeleteOwn is granted to providers (owner-scoped); Admin+ override via
            // the Tour.DeleteAny permission or an admin-tier role.
            var isAdminTier =
                AppRoles.HighestPrivilegeLevel(currentUser.Roles) >= RolePrivilegeLevel.Admin
                || currentUser.HasPermission("Permission.Tour.DeleteAny");

            if (!isAdminTier && tour.CreatedByUserId != currentUser.UserId!.Value)
            {
                return Result.Failure(
                    new Error("Tour.NotOwner", "You do not have permission to delete this tour."),
                    Outcome.Forbidden);
            }

            if (tour.BookingCount > 0)
            {
                return Result.Failure(
                    new Error(
                        "Tour.DeleteBlocked",
                        $"Tour cannot be deleted while it has {tour.BookingCount} booking(s) recorded."),
                    Outcome.Conflict);
            }

            if (tour.Status == Domain.Enums.TourStatus.Approved
                && await tourRepository.HasFutureSchedulesAsync(tour.Id, cancellationToken)
                    .ConfigureAwait(false))
            {
                return Result.Failure(
                    new Error(
                        "Tour.DeleteBlocked",
                        "Tour cannot be deleted while approved and has future schedules."),
                    Outcome.Conflict);
            }

            var capturedCreatedBy = tour.CreatedByUserId;
            var capturedPlaceId = tour.PlaceId;

            tour.SoftDelete();

            outboxWriter.Enqueue(new TourDeletedIntegrationEvent(
                TourId:          tour.Id,
                CreatedByUserId: capturedCreatedBy,
                PlaceId:         capturedPlaceId,
                DeletedAt:       tour.DeletedAt ?? DateTime.UtcNow));

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure(
                    new Error(
                        "Tour.ConcurrencyConflict",
                        "This tour was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(ContentToursCacheKeys.TagForTour(tour.Id), cancellationToken)
                .ConfigureAwait(false);
            await cache.RemoveByTagAsync(ContentToursCacheKeys.TagToursList, cancellationToken)
                .ConfigureAwait(false);
            await cache.RemoveByTagAsync(ContentToursCacheKeys.TagToursSearch, cancellationToken)
                .ConfigureAwait(false);
            await cache.RemoveByTagAsync(ContentToursCacheKeys.TagForMyTours(capturedCreatedBy), cancellationToken)
                .ConfigureAwait(false);
            if (tour.IsFeatured)
            {
                await cache.RemoveByTagAsync(ContentToursCacheKeys.TagToursFeatured, cancellationToken)
                    .ConfigureAwait(false);
            }

            logger.LogInformation(
                "Tour soft-deleted: {TourId} (CreatedBy={CreatedByUserId}, By={UserId})",
                tour.Id, capturedCreatedBy, currentUser.UserId);

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
