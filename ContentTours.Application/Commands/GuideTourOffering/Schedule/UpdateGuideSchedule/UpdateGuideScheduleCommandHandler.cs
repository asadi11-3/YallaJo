using ContentTours.Application.Caching;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Commands.GuideTourOffering.Schedule.UpdateGuideSchedule;

internal sealed class UpdateGuideScheduleCommandHandler(
    IGuideScheduleRepository scheduleRepository,
    IContentToursUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<UpdateGuideScheduleCommandHandler> logger) : ICommandHandler<UpdateGuideScheduleCommand>
{
    public async Task<Result> Handle(UpdateGuideScheduleCommand request, CancellationToken cancellationToken)
    {
        var schedule = await scheduleRepository.GetByIdAsync(request.ScheduleId, cancellationToken, asNoTracking: false);
        if (schedule is null)
            return Result.Failure(new Error("GuideSchedule.NotFound", "Schedule not found."), Outcome.NotFound);

        if (!TimeOnly.TryParse(request.StartTime, out var startTime))
            return Result.Failure(new Error("GuideSchedule.InvalidStartTime", "Invalid start time format. Use HH:mm."), Outcome.Invalid);

        TimeOnly? endTime = null;
        if (request.EndTime is not null)
        {
            if (!TimeOnly.TryParse(request.EndTime, out var parsed))
                return Result.Failure(new Error("GuideSchedule.InvalidEndTime", "Invalid end time format. Use HH:mm."), Outcome.Invalid);
            endTime = parsed;
        }

        if (request.DayOfWeek > 6)
            return Result.Failure(new Error("GuideSchedule.InvalidDayOfWeek", "DayOfWeek must be 0–6."), Outcome.Invalid);

        schedule.Update(request.DayOfWeek, startTime, endTime);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency conflict updating schedule {ScheduleId}", request.ScheduleId);
            return Result.Failure(new Error("GuideSchedule.ConcurrencyConflict", "Concurrent modification detected."), Outcome.Conflict);
        }

        await cache.RemoveByTagAsync(TourGuideCacheKeys.TagForTourOfferings(schedule.TourId), cancellationToken);
        logger.LogInformation("Updated schedule {ScheduleId}", request.ScheduleId);
        return Result.Success();
    }
}
