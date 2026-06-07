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
using YallaJo.SharedKernel.Domain.ValueObjects;

namespace ContentTours.Application.Commands.GuideTourOffering.PricingTier.CreateGuidePricingTier;

internal sealed class CreateGuidePricingTierCommandHandler(
    IGuideTourOfferingRepository offeringRepository,
    ITourGuideRepository tourGuideRepository,
    IGuidePricingTierRepository pricingTierRepository,
    IContentToursUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<CreateGuidePricingTierCommandHandler> logger) : ICommandHandler<CreateGuidePricingTierCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateGuidePricingTierCommand request, CancellationToken cancellationToken)
    {
        var offering = await offeringRepository.GetByTourAndGuideAsync(request.TourId, request.TourGuideId, cancellationToken, asNoTracking: false);
        if (offering is null)
            return Result<Guid>.Failure(new Error("GuideTourOffering.NotFound", "Guide offering not found."), Outcome.NotFound);

        var callerGuide = await tourGuideRepository.GetByUserIdAsync(currentUser.UserId!.Value, cancellationToken);
        if (callerGuide is null || offering.TourGuideId != callerGuide.Id)
            return Result<Guid>.Failure(new Error("GuideTourOffering.NotOwner", "You can only manage your own tour offerings."), Outcome.Forbidden);

        var price = new Money(request.Price, request.Currency);

        var tier = GuidePricingTier.Create(
            offering.Id, offering.TourGuideId, offering.TourId,
            request.Name, price, request.MinParticipants, request.MaxParticipants, request.Description);

        pricingTierRepository.Add(tier);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency conflict creating pricing tier for OfferingId={OfferingId}", offering.Id);
            return Result<Guid>.Failure(new Error("GuidePricingTier.ConcurrencyConflict", "Concurrent modification detected."), Outcome.Conflict);
        }

        await cache.RemoveByTagAsync(TourGuideCacheKeys.TagForTourOfferings(request.TourId), cancellationToken);
        logger.LogInformation("Created pricing tier {TierId} for offering {OfferingId}", tier.Id, offering.Id);
        return Result.Success(tier.Id);
    }
}
