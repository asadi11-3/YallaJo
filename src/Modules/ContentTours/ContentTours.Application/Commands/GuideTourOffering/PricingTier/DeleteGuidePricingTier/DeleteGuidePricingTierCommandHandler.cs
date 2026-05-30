using ContentTours.Application.Caching;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Commands.GuideTourOffering.PricingTier.DeleteGuidePricingTier;

internal sealed class DeleteGuidePricingTierCommandHandler(
    IGuidePricingTierRepository pricingTierRepository,
    IContentToursUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<DeleteGuidePricingTierCommandHandler> logger) : ICommandHandler<DeleteGuidePricingTierCommand>
{
    public async Task<Result> Handle(DeleteGuidePricingTierCommand request, CancellationToken cancellationToken)
    {
        var tier = await pricingTierRepository.GetByIdAsync(request.TierId, cancellationToken, asNoTracking: false);
        if (tier is null)
            return Result.Failure(new Error("GuidePricingTier.NotFound", "Pricing tier not found."), Outcome.NotFound);

        var tourId = tier.TourId;
        pricingTierRepository.Remove(tier);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync(TourGuideCacheKeys.TagForTourOfferings(tourId), cancellationToken);
        logger.LogInformation("Deleted pricing tier {TierId}", request.TierId);
        return Result.Success();
    }
}
