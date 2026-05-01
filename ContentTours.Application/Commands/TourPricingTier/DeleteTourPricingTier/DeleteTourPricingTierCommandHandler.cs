using ContentTours.Application.Caching;
using ContentTours.Application.Commands.TourPricingTier.Common;
using ContentTours.Application.Interfaces;
using ContentTours.Contracts;
using ContentTours.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Security.Contracts.Authorization;
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
    public async Task<Result> Handle(DeleteTourPricingTierCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var tour = await tourRepo.GetByIdAsync(request.TourId, cancellationToken);
            if (tour is null || tour.IsDeleted)
            {
                return Result.Failure(
                    new Error("Tour.NotFound", $"Tour '{request.TourId}' was not found."),
                    Outcome.NotFound);
            }

            var isAdminTier = AppRoles.HighestPrivilegeLevel(currentUser.Roles)
                >= RolePrivilegeLevel.Admin;
            if (!isAdminTier && tour.CreatedByUserId != currentUser.UserId!.Value)
            {
                return Result.Failure(
                   new Error("Tour.NotOwner", "You do not have permission to delete pricing tiers on this tour."),
                   Outcome.Forbidden);
            }

            var tier = await tierRepo.GetByIdAsync(request.TierId, cancellationToken);
            if (tier is null || tier.TourId != request.TourId)
            {
                return Result.Failure(
                    new Error("TourPricingTier.NotFound", $"Pricing tier '{request.TierId}' was not found on this tour."),
                    Outcome.NotFound);
            }

            // Adult-tier guard
            if (tier.IsAdult)
            {
                var allTiers = await tierRepo.GetAllAsync(filter: t => t.TourId == request.TourId, ct: cancellationToken);
                var guardResult = AdultTierGuard.EnsureCanRemoveOrDeactivate(tour, tier, allTiers.ToList());
                if (!guardResult.IsSuccess) return guardResult;
            }

            tierRepo.Remove(tier);

            outbox.Enqueue(new TourPricingTierChangedIntegrationEvent(
                tier.Id, tour.Id, tier.Price.Amount, tier.Currency, TourEntityChangeType.Deleted));

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(
                    ex,
                    "Concurrency conflict while {Action}: TierId={TierId} TourId={TourId}",
                    nameof(DeleteTourPricingTierCommand), request.TierId, request.TourId);

                return Result.Failure(
                    new Error(
                        "TourPricingTier.ConcurrencyConflict",
                        "This pricing tier was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(TourPricingTierCacheKeys.TagForTour(tour.Id), cancellationToken);
            await cache.RemoveByTagAsync(TourCacheKeys.TagForTour(tour.Id), cancellationToken);

            logger.LogInformation("Deleted TourPricingTier {TierId} from TourId={TourId}", tier.Id, tour.Id);

            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
