using ContentTours.Application.Caching;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Commands.GuideTourOffering.EnablePrivateTour;

internal sealed class EnablePrivateTourCommandHandler(
    IGuideTourOfferingRepository offeringRepository,
    IContentToursUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<EnablePrivateTourCommandHandler> logger) : ICommandHandler<EnablePrivateTourCommand>
{
    public async Task<Result> Handle(EnablePrivateTourCommand request, CancellationToken cancellationToken)
    {
        var offering = await offeringRepository.GetByTourAndGuideAsync(request.TourId, request.TourGuideId, cancellationToken, asNoTracking: false);
        if (offering is null)
            return Result.Failure(new Error("GuideTourOffering.NotFound", "Guide offering not found."), Outcome.NotFound);

        var result = offering.EnablePrivateTour(request.Multiplier, request.FlatPrice);
        if (result.IsFailure)
            return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync(TourGuideCacheKeys.TagForTourOfferings(request.TourId), cancellationToken);
        logger.LogInformation("Enabled private tour for offering TourId={TourId}, GuideId={GuideId}", request.TourId, request.TourGuideId);
        return Result.Success();
    }
}
