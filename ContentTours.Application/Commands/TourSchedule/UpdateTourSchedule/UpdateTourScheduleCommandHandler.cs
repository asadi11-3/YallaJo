using ContentTours.Application.Caching;
using ContentTours.Application.Commands.TourSchedule.Common;
using ContentTours.Application.Interfaces;
using ContentTours.Contracts;
using ContentTours.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Commands.TourSchedule.UpdateTourSchedule;

public sealed class UpdateTourScheduleCommandHandler(
    ITourRepository tourRepo,
    ITourScheduleRepository scheduleRepo,
    IContentToursUnitOfWork unitOfWork,
    IContentToursOutboxWriter outbox,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<UpdateTourScheduleCommandHandler> logger)
    : ICommandHandler<UpdateTourScheduleCommand>
{
    public async Task<Result> Handle(UpdateTourScheduleCommand cmd, CancellationToken ct)
    {
        var tour = await tourRepo.GetByIdAsync(cmd.TourId, ct);
        if (tour is null || tour.IsDeleted)
            return Result.NotFound("Tour.NotFound");

        if (tour.CreatedByUserId != currentUser.UserId!.Value && !currentUser.IsInRole("Admin"))
            return Result.Forbidden("Tour.NotOwner");

        var schedule = await scheduleRepo.GetByIdAsync(cmd.ScheduleId, ct);
        if (schedule is null || schedule.TourId != cmd.TourId)
            return Result.NotFound("TourSchedule.NotFound");

        // Overlap check: existing active rows minus this one + proposed change
        var existing = await scheduleRepo.GetAllAsync(
            filter: s => s.TourId == cmd.TourId && s.IsActive && s.Id != cmd.ScheduleId,
            ct: ct);

        var proposed = new[] { (cmd.DayOfWeek, cmd.StartTime, cmd.EndTime) };
        var overlapError = TourScheduleOverlapChecker.Check(existing, proposed);
        if (overlapError is not null)
            return Result.UnprocessableEntity(overlapError);

        schedule.Update(cmd.DayOfWeek, cmd.StartTime, cmd.EndTime, cmd.IsActive);

        outbox.Enqueue(new TourScheduleChangedIntegrationEvent(
            schedule.Id, cmd.TourId, cmd.DayOfWeek, cmd.StartTime, cmd.EndTime, TourEntityChangeType.Updated));

        await unitOfWork.SaveChangesAsync(ct);

        await cache.RemoveByTagAsync(TourScheduleCacheKeys.TagForTour(cmd.TourId), ct);
        await cache.RemoveByTagAsync(TourCacheKeys.TagForTour(cmd.TourId), ct);

        logger.LogInformation("Updated TourSchedule {ScheduleId} on TourId={TourId}", schedule.Id, cmd.TourId);

        return Result.Success();
    }

}
