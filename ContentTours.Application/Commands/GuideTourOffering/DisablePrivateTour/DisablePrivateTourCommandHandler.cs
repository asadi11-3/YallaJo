using ContentTours.Application.Caching;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Commands.GuideTourOffering.DisablePrivateTour;

internal sealed class DisablePrivateTourCommandHandler(
    IGuideTourOfferingRepository offeringRepository,
    IContentToursUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<DisablePrivateTourCommandHandler> logger) : ICommandHandler<DisablePrivateTourCommand>
{
    public async Task<Result> Handle(DisablePrivateTourCommand request, CancellationToken cancellationToken)
    {
        var offering = await offeringRepository.GetByTourAndGuideAsync(request.TourId, request.TourGuideId, cancellationToken, asNoTracking: false);
        if (offering is null)
            return Result.Failure(new Error("GuideTourOffering.NotFound", "Guide offering not found."), Outcome.NotFound);

        offering.DisablePrivateTour();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync(TourGuideCacheKeys.TagForTourOfferings(request.TourId), cancellationToken);
        logger.LogInformation("Disabled private tour for offering TourId={TourId}, GuideId={GuideId}", request.TourId, request.TourGuideId);
        return Result.Success();
    }
}
