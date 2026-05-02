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

namespace ContentTours.Application.Commands.TourSchedule.DeleteTourSchedule;

public sealed class DeleteTourScheduleCommandHandler(
    ITourRepository tourRepo,
    ITourScheduleRepository scheduleRepo,
    IScheduleBookingCountService bookingCountService,
    IContentToursUnitOfWork unitOfWork,
    IContentToursOutboxWriter outbox,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<DeleteTourScheduleCommandHandler> logger)
    : ICommandHandler<DeleteTourScheduleCommand>
{
    public async Task<Result> Handle(DeleteTourScheduleCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var tour = await tourRepo.GetByIdAsync(request.TourId, cancellationToken);
            if (tour is null || tour.IsDeleted)
            {
                return Result.Failure(
                   new Error("Tour.NotFound", $"Tour '{request.TourId}' was not found."),
                   Outcome.NotFound);
            }

            var isAdminTier = AppRoles.HighestPrivilegeLevel(currentUser.Roles)
                >= RolePrivilegeLevel.Admin;
            if (!isAdminTier && tour.CreatedByUserId != currentUser.UserId!.Value)
            {
                return Result.Failure(
                    new Error("Tour.NotOwner", "You do not have permission to delete schedules for this tour."),
                    Outcome.Forbidden);
            }

            var schedule = await scheduleRepo.GetByIdAsync(request.ScheduleId, cancellationToken);
            if (schedule is null || schedule.TourId != request.TourId)
            {
                return Result.Failure(
                   new Error("TourSchedule.NotFound", $"Schedule '{request.ScheduleId}' was not found on this tour."),
                   Outcome.NotFound);
            }

            // Cross-module deletion guard
            var futureBookings = await bookingCountService
                .GetFutureBookingCountForScheduleAsync(request.ScheduleId, cancellationToken);

            if (futureBookings > 0)
            {
                return Result.Fail(
                   Outcome.Conflict,
                   new Error(
                       "TourSchedule.DeleteBlocked",
                       $"{futureBookings} future booking(s) reference this schedule."));
            }

            scheduleRepo.Remove(schedule);

            outbox.Enqueue(new TourScheduleChangedIntegrationEvent(
                schedule.Id, request.TourId, schedule.DayOfWeek, schedule.StartTime, schedule.EndTime, TourEntityChangeType.Deleted));

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(
                    ex,
                    "Concurrency conflict while {Action}: ScheduleId={ScheduleId} TourId={TourId}",
                    nameof(DeleteTourScheduleCommand), request.ScheduleId, request.TourId);

                return Result.Failure(
                    new Error(
                        "TourSchedule.ConcurrencyConflict",
                        "This schedule was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(TourScheduleCacheKeys.TagForTour(request.TourId), cancellationToken);
            await cache.RemoveByTagAsync(ContentToursCacheKeys.TagForTour(request.TourId), cancellationToken);

            logger.LogInformation("Deleted TourSchedule {ScheduleId} from TourId={TourId}", schedule.Id, request.TourId);

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
