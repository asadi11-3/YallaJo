using ContentTours.Application.Caching;
using ContentTours.Application.Interfaces;
using ContentTours.Contracts.IntegrationEvents;
using ContentTours.Domain.Enums;
using ContentTours.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Domain.ValueObjects;

namespace ContentTours.Application.Commands.TourPackage.UpdateTourPackage;

public sealed class UpdateTourPackageCommandHandler(
    ITourPackageRepository packageRepository,
    ITourRepository tourRepository,
    ITourCapacityService capacityService,
    IContentToursUnitOfWork unitOfWork,
    IContentToursOutboxWriter outbox,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<UpdateTourPackageCommandHandler> logger)
    : ICommandHandler<UpdateTourPackageCommand>
{
    public async Task<Result> Handle(
        UpdateTourPackageCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            

            var callerId = currentUser.UserId!.Value;

            var package = await packageRepository
                .GetByIdWithDetailsAsync(request.Id, cancellationToken)
                .ConfigureAwait(false);

            if (package is null)
            {
                return Result.Failure(
                    new Error("TourPackage.NotFound", $"Tour package '{request.Id}' was not found."),
                    Outcome.NotFound);
            }

            // Owner-or-admin gate (canonical project pattern).
            if (package.CreatedByUserId != callerId)
            {
                return Result.Failure(
                    new Error(
                        "TourPackage.NotOwner",
                        "You do not have permission to update this tour package."),
                    Outcome.Forbidden);
            }

            // ── ValidFrom immutability (PDF: 409) ──────────────────────────────
            if (request.ValidFrom.HasValue && request.ValidFrom != package.ValidFrom)
            {
                return Result.Failure(
                    new Error(
                        "TourPackage.ValidFromImmutable",
                        "ValidFrom cannot be changed after the package is created."),
                    Outcome.Conflict);
            }

            // ── IncludedTourIds shape ──────────────────────────────────────────
            var rawIds = request.IncludedTourIds ?? Array.Empty<Guid>();
            var nonEmpty = rawIds.Where(id => id != Guid.Empty).ToList();
            var distinctIds = nonEmpty.Distinct().ToList();

            if (distinctIds.Count < 2)
            {
                return Result.Failure(
                    new Error(
                        "TourPackage.InsufficientInclusions",
                        "A tour package must include at least 2 distinct tours."),
                    Outcome.Invalid);
            }

            if (distinctIds.Count != nonEmpty.Count)
            {
                return Result.Failure(
                    new Error(
                        "TourPackage.DuplicateInclusions",
                        "IncludedTourIds must not contain duplicate values."),
                    Outcome.Invalid);
            }

            var previousTourIds = package.IncludedTours.Select(x => x.TourId).ToList();
            var newTourSet = distinctIds.ToHashSet();
            var oldTourSet = previousTourIds.ToHashSet();
            var toursChanged = !oldTourSet.SetEquals(newTourSet);

            // Currency must always be revalidated (price/currency are mutable).
            if (string.IsNullOrWhiteSpace(request.Currency) || request.Currency.Length != 3)
            {
                return Result.Failure(
                    new Error(
                        "TourPackage.CurrencyMismatch",
                        "Currency must be a 3-letter ISO code that matches every included tour."),
                    Outcome.Invalid);
            }

            var normalizedCurrency = request.Currency.ToUpperInvariant();

            // Bulk-load tours (always — needed for currency/capacity revalidation).
            var tours = await tourRepository
                .GetAllAsync(
                    filter: t => distinctIds.Contains(t.Id),
                    ct: cancellationToken,
                    asNoTracking: true)
                .ConfigureAwait(false);

            if (tours.Count != distinctIds.Count)
            {
                var missing = distinctIds.Except(tours.Select(t => t.Id)).ToList();
                return Result.Failure(
                    new Error(
                        "TourPackage.IncludesUnknownTour",
                        $"One or more included tours do not exist: {string.Join(", ", missing)}."),
                    Outcome.Invalid);
            }

            if (tours.Any(t => t.IsDeleted))
            {
                return Result.Failure(
                    new Error(
                        "TourPackage.IncludesDeletedTour",
                        "One or more included tours have been deleted."),
                    Outcome.Invalid);
            }

            if (tours.Any(t => t.Status != TourStatus.Approved))
            {
                return Result.Failure(
                    new Error(
                        "TourPackage.IncludesNonApprovedTour",
                        "Every included tour must be in the Approved status."),
                    Outcome.UnprocessableEntity);
            }

            if (tours.Any(t => t.CreatedByUserId != callerId))
            {
                return Result.Failure(
                    new Error(
                        "TourPackage.NotOwnerOfAllTours",
                        "You do not own every included tour."),
                    Outcome.Forbidden);
            }

            if (tours.Any(t => !string.Equals(t.Currency, normalizedCurrency, StringComparison.OrdinalIgnoreCase)))
            {
                return Result.Failure(
                    new Error(
                        "TourPackage.CurrencyMismatch",
                        "Package currency must match the currency of every included tour."),
                    Outcome.Invalid);
            }

            if (request.MaxParticipants is { } cap)
            {
                var capLimit = tours.Min(t => t.MaxGroupSize);
                if (cap > capLimit)
                {
                    return Result.Failure(
                        new Error(
                            "TourPackage.MaxParticipantsExceedsTour",
                            $"MaxParticipants ({cap}) exceeds the smallest included tour group size ({capLimit})."),
                        Outcome.Invalid);
                }
            }

            if (request.ValidTo.HasValue && package.ValidFrom.HasValue
                && package.ValidFrom.Value >= request.ValidTo.Value)
            {
                return Result.Failure(
                    new Error(
                        "TourPackage.InvalidValidityWindow",
                        "ValidTo must be after the existing ValidFrom."),
                    Outcome.Invalid);
            }

            // Atomic capacity recheck only when bundle membership or capacity intent changed.
            var maxParticipantsChanged = request.MaxParticipants != package.MaxParticipants;
            if (toursChanged || maxParticipantsChanged)
            {
                var hasCapacity = await capacityService
                    .AllHaveCapacityAsync(distinctIds, request.MaxParticipants, cancellationToken)
                    .ConfigureAwait(false);
                if (!hasCapacity)
                {
                    return Result.Failure(
                        new Error(
                            "TourPackage.AtomicBookingFailed",
                            "One or more included tours could not reserve capacity for this package."),
                        Outcome.Conflict);
                }
            }

            // Apply mutation (Domain replaces the included-tour link set).
            var newPrice = new Money(request.Price, normalizedCurrency);
            package.Update(
                name: request.Name,
                description: request.Description,
                price: newPrice,
                currency: normalizedCurrency,
                maxParticipants: request.MaxParticipants,
                validTo: request.ValidTo,
                includedTourIds: distinctIds);

            // Wire RowVersion for optimistic concurrency. The package is already tracked
            // (loaded above), so override the original concurrency token before SaveChanges.
            packageRepository.AttachAndMarkModifiedWithConcurrency(
                package,
                concurrencyPropertyName: nameof(Domain.Entities.TourPackage.RowVersion),
                originalConcurrencyValue: request.RowVersion);

            // Outbox enqueue BEFORE save.
            outbox.Enqueue(new TourPackageUpdatedIntegrationEvent(
                PackageId:               package.Id,
                UpdatedByUserId:         callerId,
                Name:                    package.Name,
                Price:                   package.Price.Amount,
                Currency:                package.Currency,
                MaxParticipants:         package.MaxParticipants,
                ValidFrom:               package.ValidFrom,
                ValidTo:                 package.ValidTo,
                IncludedTourIds:         distinctIds,
                PreviousIncludedTourIds: previousTourIds));

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex,
                    "Concurrency conflict updating TourPackage {PackageId}", package.Id);
                return Result.Failure(
                    new Error(
                        "TourPackage.ConcurrencyConflict",
                        "The package was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            // Cache invalidation: package detail + list + every tour in the union of old+new.
            await cache.RemoveByTagAsync(ContentToursCacheKeys.TagForPackage(package.Id), cancellationToken)
                .ConfigureAwait(false);
            await cache.RemoveByTagAsync(ContentToursCacheKeys.TagPackagesList, cancellationToken)
                .ConfigureAwait(false);
            foreach (var tourId in oldTourSet.Union(newTourSet))
            {
                await cache.RemoveByTagAsync(ContentToursCacheKeys.TagForTour(tourId), cancellationToken)
                    .ConfigureAwait(false);
            }

            logger.LogInformation(
                "TourPackage {PackageId} updated (Tours={TourCount}, By={UserId})",
                package.Id, distinctIds.Count, callerId);

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
