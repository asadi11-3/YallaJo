using ContentTours.Application.Caching;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Commands.GuideTourOffering.RemoveGuideOffering;

internal sealed class RemoveGuideOfferingCommandHandler(
    IGuideTourOfferingRepository offeringRepository,
    ITourGuideRepository tourGuideRepository,
    IContentToursUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<RemoveGuideOfferingCommandHandler> logger) : ICommandHandler<RemoveGuideOfferingCommand>
{
    public async Task<Result> Handle(RemoveGuideOfferingCommand request, CancellationToken cancellationToken)
    {
        var offering = await offeringRepository.GetByTourAndGuideAsync(request.TourId, request.TourGuideId, cancellationToken, asNoTracking: false);
        if (offering is null)
            return Result.Failure(new Error("GuideTourOffering.NotFound", "Guide offering not found."), Outcome.NotFound);

        var callerGuide = await tourGuideRepository.GetByUserIdAsync(currentUser.UserId!.Value, cancellationToken);
        if (callerGuide is null || offering.TourGuideId != callerGuide.Id)
            return Result.Failure(new Error("GuideTourOffering.NotOwner", "You can only manage your own tour offerings."), Outcome.Forbidden);

        var result = offering.Remove();
        if (result.IsFailure)
            return result;

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(
                new Error("GuideTourOffering.ConcurrencyConflict", "Concurrent update detected."),
                Outcome.Conflict);
        }

        await cache.RemoveByTagAsync(TourGuideCacheKeys.TagForTourOfferings(request.TourId), cancellationToken);
        await cache.RemoveByTagAsync(TourGuideCacheKeys.TagForGuideOfferings(request.TourGuideId), cancellationToken);
        logger.LogInformation("Removed offering TourId={TourId}, GuideId={GuideId}", request.TourId, request.TourGuideId);
        return Result.Success();
    }
}
