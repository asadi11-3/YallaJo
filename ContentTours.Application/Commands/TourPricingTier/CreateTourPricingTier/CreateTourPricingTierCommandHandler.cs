using ContentTours.Application.Caching;
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
        CreateTourPricingTierCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var tour = await tourRepo.GetByIdAsync(request.TourId, cancellationToken);
            if (tour is null || tour.IsDeleted)
            {
                return Result<CreateTourPricingTierResult>.Failure(
                   new Error("Tour.NotFound", $"Tour '{request.TourId}' was not found."),
                   Outcome.NotFound);
            }

            var isAdminTier = AppRoles.HighestPrivilegeLevel(currentUser.Roles)
                >= RolePrivilegeLevel.Admin;
            if (!isAdminTier && tour.CreatedByUserId != currentUser.UserId!.Value) {
                return Result<CreateTourPricingTierResult>.Failure(
                  new Error("Tour.NotOwner", "You do not have permission to add pricing tiers to this tour."),
                  Outcome.Forbidden);
            }

            if (!request.Currency.Equals(tour.Currency, StringComparison.OrdinalIgnoreCase))
            {
                return Result.Invalid<CreateTourPricingTierResult>(
                   new Error(
                       "TourPricingTier.CurrencyMismatch",
                       $"Tier currency '{request.Currency}' does not match tour currency '{tour.Currency}'."));
            }

            var existingTiers = await tierRepo.GetAllAsync(
                filter: t => t.TourId == request.TourId,
                ct: cancellationToken);

            var nameConflict = existingTiers.Any(t =>
                t.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase));
            if (nameConflict)
            {
                return Result<CreateTourPricingTierResult>.Failure(
                   new Error(
                       "TourPricingTier.NameConflict",
                       $"A tier named '{request.Name}' already exists on this tour."),
                   Outcome.Conflict);
            }

            var price = new Money(request.Price, request.Currency);
            var tier = ContentTours.Domain.Entities.TourPricingTier.Create(
                request.TourId, request.Name, request.Description,
                price, request.ParticipantType,
                request.MinParticipants, request.MaxParticipants);

            await tierRepo.AddAsync(tier, cancellationToken);

            outbox.Enqueue(new TourPricingTierChangedIntegrationEvent(
                tier.Id, tour.Id, tier.Price.Amount, tier.Currency, TourEntityChangeType.Created));

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(
                    ex,
                    "Concurrency conflict while {Action}: TourId={TourId}",
                    nameof(CreateTourPricingTierCommand), request.TourId);

                return Result<CreateTourPricingTierResult>.Failure(
                    new Error(
                        "TourPricingTier.ConcurrencyConflict",
                        "This pricing tier was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(TourPricingTierCacheKeys.TagForTour(tour.Id), cancellationToken);
            await cache.RemoveByTagAsync(ContentToursCacheKeys.TagForTour(tour.Id), cancellationToken);

            logger.LogInformation(
                "Created TourPricingTier {TierId} '{Name}' on TourId={TourId} at {Price} {Currency}",
                tier.Id, tier.Name, tour.Id, tier.Price.Amount, tier.Currency);

            return Result.Created(new CreateTourPricingTierResult(tier.Id, tier.Name));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<CreateTourPricingTierResult>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
