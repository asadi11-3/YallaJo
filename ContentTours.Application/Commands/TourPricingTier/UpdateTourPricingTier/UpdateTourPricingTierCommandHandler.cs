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
using YallaJo.SharedKernel.Domain.ValueObjects;

namespace ContentTours.Application.Commands.TourPricingTier.UpdateTourPricingTier;

public sealed class UpdateTourPricingTierCommandHandler(
    ITourRepository tourRepo,
    ITourPricingTierRepository tierRepo,
    IContentToursUnitOfWork unitOfWork,
    IContentToursOutboxWriter outbox,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<UpdateTourPricingTierCommandHandler> logger)
    : ICommandHandler<UpdateTourPricingTierCommand>
{
    public async Task<Result> Handle(UpdateTourPricingTierCommand request, CancellationToken cancellationToken)
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
                   new Error("Tour.NotOwner", "You do not have permission to update pricing tiers on this tour."),
                   Outcome.Forbidden);
            }

            if (!request.Currency.Equals(tour.Currency, StringComparison.OrdinalIgnoreCase))
            {
                return Result.Invalid(
                   new Error(
                       "TourPricingTier.CurrencyMismatch",
                       $"Tier currency '{request.Currency}' does not match tour currency '{tour.Currency}'."));
            }

            var tier = await tierRepo.GetByIdAsync(request.TierId, cancellationToken);
            if (tier is null || tier.TourId != request.TourId)
            {
                return Result.Failure(
                    new Error("TourPricingTier.NotFound", $"Pricing tier '{request.TierId}' was not found on this tour."),
                    Outcome.NotFound);
            }

            if (!tier.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase))
            {
                var allTiers = await tierRepo.GetAllAsync(filter: t => t.TourId == request.TourId, ct: cancellationToken);
                var conflict = allTiers.Any(t =>
                    t.Id != request.TierId && t.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase));
                if (conflict)
                {
                    return Result.Failure(
                       new Error(
                           "TourPricingTier.NameConflict",
                           $"A tier named '{request.Name}' already exists on this tour."),
                       Outcome.Conflict);
                }
            }

            var isChangingAwayFromAdult = tier.IsAdult
                && request.ParticipantType != ContentTours.Domain.Enums.ParticipantType.Adult;
            var isDeactivatingAdult = !request.IsActive && tier.IsAdult;

            if (isChangingAwayFromAdult || isDeactivatingAdult)
            {
                var allTiers = await tierRepo.GetAllAsync(filter: t => t.TourId == request.TourId, ct: cancellationToken);
                var guardResult = AdultTierGuard.EnsureCanRemoveOrDeactivate(tour, tier, allTiers.ToList());
                if (!guardResult.IsSuccess) return guardResult;
            }

            var changeType = !request.IsActive && tier.IsActive
                ? TourEntityChangeType.Deactivated
                : TourEntityChangeType.Updated;
            tier.Update(request.Name, request.Description, new Money(request.Price, request.Currency),
                request.ParticipantType, request.MinParticipants, request.MaxParticipants, request.IsActive);

            outbox.Enqueue(new TourPricingTierChangedIntegrationEvent(
                tier.Id, tour.Id, tier.Price.Amount, tier.Currency, changeType));

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(
                    ex,
                    "Concurrency conflict while {Action}: TierId={TierId} TourId={TourId}",
                    nameof(UpdateTourPricingTierCommand), request.TierId, request.TourId);

                return Result.Failure(
                    new Error(
                        "TourPricingTier.ConcurrencyConflict",
                        "This pricing tier was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(TourPricingTierCacheKeys.TagForTour(tour.Id), cancellationToken);
            await cache.RemoveByTagAsync(ContentToursCacheKeys.TagForTour(tour.Id), cancellationToken);

            logger.LogInformation("Updated TourPricingTier {TierId} on TourId={TourId}", tier.Id, tour.Id);

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
