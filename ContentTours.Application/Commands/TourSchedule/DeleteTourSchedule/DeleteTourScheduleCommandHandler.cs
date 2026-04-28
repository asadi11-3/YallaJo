using ContentTours.Application.Caching;
using ContentTours.Application.Interfaces;
using ContentTours.Contracts;
using ContentTours.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
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
    public async Task<Result> Handle(DeleteTourScheduleCommand cmd, CancellationToken ct)
    {
        var tour = await tourRepo.GetByIdAsync(cmd.TourId, ct);
        if (tour is null || tour.IsDeleted)
            return Result.NotFound("Tour.NotFound");

        if (tour.CreatedByUserId != currentUser.UserId!.Value && !currentUser.IsInRole("Admin"))
            return Result.Forbidden("Tour.NotOwner");

        var schedule = await scheduleRepo.GetByIdAsync(cmd.ScheduleId, ct);
        if (schedule is null || schedule.TourId != cmd.TourId)
            return Result.NotFound("TourSchedule.NotFound");

        // Cross-module deletion guard
        var futureBookings = await bookingCountService
            .GetFutureBookingCountForScheduleAsync(cmd.ScheduleId, ct);

        if (futureBookings > 0)
            return Result.Fail(
                Outcome.Conflict,
                new Error("TourSchedule.DeleteBlocked",
                    $"{futureBookings} future booking(s) reference this schedule."));

        scheduleRepo.Remove(schedule);

        outbox.Enqueue(new TourScheduleChangedIntegrationEvent(
            schedule.Id, cmd.TourId, schedule.DayOfWeek, schedule.StartTime, schedule.EndTime, TourEntityChangeType.Deleted));

        await unitOfWork.SaveChangesAsync(ct);

        await cache.RemoveByTagAsync(TourScheduleCacheKeys.TagForTour(cmd.TourId), ct);
        await cache.RemoveByTagAsync(TourCacheKeys.TagForTour(cmd.TourId), ct);

        logger.LogInformation("Deleted TourSchedule {ScheduleId} from TourId={TourId}", schedule.Id, cmd.TourId);

        return Result.Success();
    }
}
