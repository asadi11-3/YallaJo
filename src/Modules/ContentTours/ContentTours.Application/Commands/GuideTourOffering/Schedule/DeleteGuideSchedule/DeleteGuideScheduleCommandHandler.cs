using ContentTours.Application.Caching;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Commands.GuideTourOffering.Schedule.DeleteGuideSchedule;

internal sealed class DeleteGuideScheduleCommandHandler(
    IGuideScheduleRepository scheduleRepository,
    IContentToursUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<DeleteGuideScheduleCommandHandler> logger) : ICommandHandler<DeleteGuideScheduleCommand>
{
    public async Task<Result> Handle(DeleteGuideScheduleCommand request, CancellationToken cancellationToken)
    {
        var schedule = await scheduleRepository.GetByIdAsync(request.ScheduleId, cancellationToken, asNoTracking: false);
        if (schedule is null)
            return Result.Failure(new Error("GuideSchedule.NotFound", "Schedule not found."), Outcome.NotFound);

        var tourId = schedule.TourId;
        scheduleRepository.Remove(schedule);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync(TourGuideCacheKeys.TagForTourOfferings(tourId), cancellationToken);
        logger.LogInformation("Deleted schedule {ScheduleId}", request.ScheduleId);
        return Result.Success();
    }
}
