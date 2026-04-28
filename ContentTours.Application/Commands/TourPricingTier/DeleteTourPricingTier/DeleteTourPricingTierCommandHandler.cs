using ContentTours.Application.Caching;
using ContentTours.Application.Commands.TourPricingTier.Common;
using ContentTours.Application.Interfaces;
using ContentTours.Contracts;
using ContentTours.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Commands.TourPricingTier.DeleteTourPricingTier;

public sealed class DeleteTourPricingTierCommandHandler(
    ITourRepository tourRepo,
    ITourPricingTierRepository tierRepo,
    IContentToursUnitOfWork unitOfWork,
    IContentToursOutboxWriter outbox,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<DeleteTourPricingTierCommandHandler> logger)
    : ICommandHandler<DeleteTourPricingTierCommand>
{
    public async Task<Result> Handle(DeleteTourPricingTierCommand cmd, CancellationToken ct)
    {
        var tour = await tourRepo.GetByIdAsync(cmd.TourId, ct);
        if (tour is null || tour.IsDeleted)
            return Result.NotFound("Tour.NotFound");

        if (tour.CreatedByUserId != currentUser.UserId!.Value && !currentUser.IsInRole("Admin"))
            return Result.Forbidden("Tour.NotOwner");

        var tier = await tierRepo.GetByIdAsync(cmd.TierId, ct);
        if (tier is null || tier.TourId != cmd.TourId)
            return Result.NotFound("TourPricingTier.NotFound");

        // Adult-tier guard
        if (tier.IsAdult)
        {
            var allTiers = await tierRepo.GetAllAsync(filter: t => t.TourId == cmd.TourId, ct: ct);
            var guardResult = AdultTierGuard.EnsureCanRemoveOrDeactivate(tour, tier, allTiers.ToList());
            if (!guardResult.IsSuccess) return guardResult;
        }

        tierRepo.Remove(tier);

        outbox.Enqueue(new TourPricingTierChangedIntegrationEvent(
            tier.Id, tour.Id, tier.Price.Amount, tier.Currency, TourEntityChangeType.Deleted));

        await unitOfWork.SaveChangesAsync(ct);

        await cache.RemoveByTagAsync(TourPricingTierCacheKeys.TagForTour(tour.Id), ct);
        await cache.RemoveByTagAsync(TourCacheKeys.TagForTour(tour.Id), ct);

        logger.LogInformation("Deleted TourPricingTier {TierId} from TourId={TourId}", tier.Id, tour.Id);

        return Result.Success();
    }
}
