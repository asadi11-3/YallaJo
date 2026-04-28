using ContentTours.Application.Caching;
using ContentTours.Application.Interfaces;
using ContentTours.Contracts;
using ContentTours.Domain.Entities;
using ContentTours.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Domain.ValueObjects;

namespace ContentTours.Application.Commands.TourPricingTier.CreateTourPricingTier;

public sealed class CreateTourPricingTierCommandHandler(
    ITourRepository tourRepo,
    ITourPricingTierRepository tierRepo,
    IContentToursUnitOfWork unitOfWork,
    IContentToursOutboxWriter outbox,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<CreateTourPricingTierCommandHandler> logger)
    : ICommandHandler<CreateTourPricingTierCommand, CreateTourPricingTierResult>
{
    public async Task<Result<CreateTourPricingTierResult>> Handle(
        CreateTourPricingTierCommand cmd, CancellationToken ct)
    {
        // 1. Load parent tour
        var tour = await tourRepo.GetByIdAsync(cmd.TourId, ct);
        if (tour is null || tour.IsDeleted)
            return Result.NotFound<CreateTourPricingTierResult>("Tour.NotFound");

        // 2. Owner-or-admin check
        if (tour.CreatedByUserId != currentUser.UserId!.Value && !currentUser.IsInRole("Admin"))
            return Result.Forbidden<CreateTourPricingTierResult>("Tour.NotOwner");

        // 3. Currency match
        if (!cmd.Currency.Equals(tour.Currency, StringComparison.OrdinalIgnoreCase))
            return Result.Invalid<CreateTourPricingTierResult>(
                new Error("TourPricingTier.CurrencyMismatch",
                    $"Tier currency '{cmd.Currency}' does not match tour currency '{tour.Currency}'."));

        // 4. Name uniqueness (case-insensitive)
        var existingTiers = await tierRepo.GetAllAsync(
            filter: t => t.TourId == cmd.TourId,
            ct: ct);

        var nameConflict = existingTiers.Any(t =>
            t.Name.Equals(cmd.Name, StringComparison.OrdinalIgnoreCase));
        if (nameConflict)
            return Result.Conflict<CreateTourPricingTierResult>(
                $"TourPricingTier.NameConflict: A tier named '{cmd.Name}' already exists on this tour.");

        // 5. Create entity — currency embedded in Money, ParticipantType is typed enum
        var price = new Money(cmd.Price, cmd.Currency);
        var tier = ContentTours.Domain.Entities.TourPricingTier.Create(
            cmd.TourId, cmd.Name, cmd.Description,
            price, cmd.ParticipantType,
            cmd.MinParticipants, cmd.MaxParticipants);

        await tierRepo.AddAsync(tier, ct);

        // 6. Outbox event (BEFORE SaveChanges — atomic)
        outbox.Enqueue(new TourPricingTierChangedIntegrationEvent(
            tier.Id, tour.Id, tier.Price.Amount, tier.Currency, TourEntityChangeType.Created));

        await unitOfWork.SaveChangesAsync(ct);

        // 7. Cache invalidation
        await cache.RemoveByTagAsync(TourPricingTierCacheKeys.TagForTour(tour.Id), ct);
        await cache.RemoveByTagAsync(TourCacheKeys.TagForTour(tour.Id), ct);

        logger.LogInformation(
            "Created TourPricingTier {TierId} '{Name}' on TourId={TourId} at {Price} {Currency}",
            tier.Id, tier.Name, tour.Id, tier.Price.Amount, tier.Currency);

        return Result.Created(new CreateTourPricingTierResult(tier.Id, tier.Name));
    }
}
