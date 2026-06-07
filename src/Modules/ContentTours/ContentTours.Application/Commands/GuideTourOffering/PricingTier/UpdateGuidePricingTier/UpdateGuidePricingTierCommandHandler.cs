using ContentTours.Application.Caching;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Domain.ValueObjects;

namespace ContentTours.Application.Commands.GuideTourOffering.PricingTier.UpdateGuidePricingTier;

internal sealed class UpdateGuidePricingTierCommandHandler(
    IGuidePricingTierRepository pricingTierRepository,
    ITourGuideRepository tourGuideRepository,
    IContentToursUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<UpdateGuidePricingTierCommandHandler> logger) : ICommandHandler<UpdateGuidePricingTierCommand>
{
    public async Task<Result> Handle(UpdateGuidePricingTierCommand request, CancellationToken cancellationToken)
    {
        var tier = await pricingTierRepository.GetByIdAsync(request.TierId, cancellationToken, asNoTracking: false);
        if (tier is null)
            return Result.Failure(new Error("GuidePricingTier.NotFound", "Pricing tier not found."), Outcome.NotFound);

        var callerGuide = await tourGuideRepository.GetByUserIdAsync(currentUser.UserId!.Value, cancellationToken);
        if (callerGuide is null || tier.TourGuideId != callerGuide.Id)
            return Result.Failure(new Error("GuideTourOffering.NotOwner", "You can only manage your own tour offerings."), Outcome.Forbidden);

        var price = new Money(request.Price, request.Currency);
        tier.Update(request.Name, price, request.MinParticipants, request.MaxParticipants, request.Description);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency conflict updating tier {TierId}", request.TierId);
            return Result.Failure(new Error("GuidePricingTier.ConcurrencyConflict", "Concurrent modification detected."), Outcome.Conflict);
        }

        await cache.RemoveByTagAsync(TourGuideCacheKeys.TagForTourOfferings(tier.TourId), cancellationToken);
        logger.LogInformation("Updated pricing tier {TierId}", request.TierId);
        return Result.Success();
    }
}
