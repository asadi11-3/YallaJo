using ContentTours.Application.Caching;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Commands.GuideTourOffering.Schedule.DeleteGuideSchedule;

internal sealed class DeleteGuideScheduleCommandHandler(
    IGuideScheduleRepository scheduleRepository,
    ITourGuideRepository tourGuideRepository,
    IContentToursUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<DeleteGuideScheduleCommandHandler> logger) : ICommandHandler<DeleteGuideScheduleCommand>
{
    public async Task<Result> Handle(DeleteGuideScheduleCommand request, CancellationToken cancellationToken)
    {
        var schedule = await scheduleRepository.GetByIdAsync(request.ScheduleId, cancellationToken, asNoTracking: false);
        if (schedule is null)
            return Result.Failure(new Error("GuideSchedule.NotFound", "Schedule not found."), Outcome.NotFound);

        var callerGuide = await tourGuideRepository.GetByUserIdAsync(currentUser.UserId!.Value, cancellationToken);
        if (callerGuide is null || schedule.TourGuideId != callerGuide.Id)
            return Result.Failure(new Error("GuideTourOffering.NotOwner", "You can only manage your own tour offerings."), Outcome.Forbidden);

        var tourId = schedule.TourId;
        scheduleRepository.Remove(schedule);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync(TourGuideCacheKeys.TagForTourOfferings(tourId), cancellationToken);
        logger.LogInformation("Deleted schedule {ScheduleId}", request.ScheduleId);
        return Result.Success();
    }
}
