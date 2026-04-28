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
    public async Task<Result> Handle(UpdateTourPricingTierCommand cmd, CancellationToken ct)
    {
        var tour = await tourRepo.GetByIdAsync(cmd.TourId, ct);
        if (tour is null || tour.IsDeleted)
            return Result.NotFound("Tour.NotFound");

        if (tour.CreatedByUserId != currentUser.UserId!.Value && !currentUser.IsInRole("Admin"))
            return Result.Forbidden("Tour.NotOwner");

        if (!cmd.Currency.Equals(tour.Currency, StringComparison.OrdinalIgnoreCase))
            return Result.Invalid(
                new Error("TourPricingTier.CurrencyMismatch",
                    $"Tier currency '{cmd.Currency}' does not match tour currency '{tour.Currency}'."));

        var tier = await tierRepo.GetByIdAsync(cmd.TierId, ct);
        if (tier is null || tier.TourId != cmd.TourId)
            return Result.NotFound("TourPricingTier.NotFound");

        // Name uniqueness check (if name changed)
        if (!tier.Name.Equals(cmd.Name, StringComparison.OrdinalIgnoreCase))
        {
            var allTiers = await tierRepo.GetAllAsync(filter: t => t.TourId == cmd.TourId, ct: ct);
            var conflict = allTiers.Any(t =>
                t.Id != cmd.TierId && t.Name.Equals(cmd.Name, StringComparison.OrdinalIgnoreCase));
            if (conflict)
                return Result.Conflict($"TourPricingTier.NameConflict: A tier named '{cmd.Name}' already exists.");
        }

        // Adult-tier guard — fires on:
        //   (a) deactivating an Adult tier, OR
        //   (b) changing ParticipantType away from Adult (removes the Adult concept)
        var isChangingAwayFromAdult = tier.IsAdult
            && cmd.ParticipantType != ContentTours.Domain.Enums.ParticipantType.Adult;
        var isDeactivatingAdult = !cmd.IsActive && tier.IsAdult;

        if (isChangingAwayFromAdult || isDeactivatingAdult)
        {
            var allTiers = await tierRepo.GetAllAsync(filter: t => t.TourId == cmd.TourId, ct: ct);
            var guardResult = AdultTierGuard.EnsureCanRemoveOrDeactivate(tour, tier, allTiers.ToList());
            if (!guardResult.IsSuccess) return guardResult;
        }

        var changeType = !cmd.IsActive && tier.IsActive
            ? TourEntityChangeType.Deactivated
            : TourEntityChangeType.Updated;
        tier.Update(cmd.Name, cmd.Description, new Money(cmd.Price, cmd.Currency),
            cmd.ParticipantType, cmd.MinParticipants, cmd.MaxParticipants, cmd.IsActive);

        outbox.Enqueue(new TourPricingTierChangedIntegrationEvent(
            tier.Id, tour.Id, tier.Price.Amount, tier.Currency, changeType));

        await unitOfWork.SaveChangesAsync(ct);

        await cache.RemoveByTagAsync(TourPricingTierCacheKeys.TagForTour(tour.Id), ct);
        await cache.RemoveByTagAsync(TourCacheKeys.TagForTour(tour.Id), ct);

        logger.LogInformation("Updated TourPricingTier {TierId} on TourId={TourId}", tier.Id, tour.Id);

        return Result.Success();
    }
}
