using ContentTours.Application.Caching;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Entities;
using ContentTours.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Commands.GuideTourOffering.Schedule.CreateGuideSchedule;

internal sealed class CreateGuideScheduleCommandHandler(
    IGuideTourOfferingRepository offeringRepository,
    ITourGuideRepository tourGuideRepository,
    IGuideScheduleRepository scheduleRepository,
    IContentToursUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<CreateGuideScheduleCommandHandler> logger) : ICommandHandler<CreateGuideScheduleCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateGuideScheduleCommand request, CancellationToken cancellationToken)
    {
        var offering = await offeringRepository.GetByTourAndGuideAsync(request.TourId, request.TourGuideId, cancellationToken, asNoTracking: false);
        if (offering is null)
            return Result<Guid>.Failure(new Error("GuideTourOffering.NotFound", "Guide offering not found for this tour and guide."), Outcome.NotFound);

        var callerGuide = await tourGuideRepository.GetByUserIdAsync(currentUser.UserId!.Value, cancellationToken);
        if (callerGuide is null || offering.TourGuideId != callerGuide.Id)
            return Result<Guid>.Failure(new Error("GuideTourOffering.NotOwner", "You can only manage your own tour offerings."), Outcome.Forbidden);

        if (!TimeOnly.TryParse(request.StartTime, out var startTime))
            return Result<Guid>.Failure(new Error("GuideSchedule.InvalidStartTime", "Invalid start time format. Use HH:mm."), Outcome.Invalid);

        TimeOnly? endTime = null;
        if (request.EndTime is not null)
        {
            if (!TimeOnly.TryParse(request.EndTime, out var parsed))
                return Result<Guid>.Failure(new Error("GuideSchedule.InvalidEndTime", "Invalid end time format. Use HH:mm."), Outcome.Invalid);
            endTime = parsed;
        }

        if (request.DayOfWeek > 6)
            return Result<Guid>.Failure(new Error("GuideSchedule.InvalidDayOfWeek", "DayOfWeek must be 0 (Sunday) through 6 (Saturday)."), Outcome.Invalid);

        var schedule = GuideSchedule.Create(
            offering.Id, offering.TourGuideId, offering.TourId,
            request.DayOfWeek, startTime, endTime);

        scheduleRepository.Add(schedule);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency conflict creating schedule for OfferingId={OfferingId}", offering.Id);
            return Result<Guid>.Failure(new Error("GuideSchedule.ConcurrencyConflict", "Concurrent modification detected. Please retry."), Outcome.Conflict);
        }

        await cache.RemoveByTagAsync(TourGuideCacheKeys.TagForTourOfferings(request.TourId), cancellationToken);
        logger.LogInformation("Created schedule {ScheduleId} for offering {OfferingId}", schedule.Id, offering.Id);
        return Result.Success(schedule.Id);
    }
}
