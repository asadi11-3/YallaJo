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

using TourPackageEntity = ContentTours.Domain.Entities.TourPackage;

namespace ContentTours.Application.Commands.TourPackage.CreateTourPackage;

public sealed class CreateTourPackageCommandHandler(
    ITourPackageRepository packageRepository,
    ITourRepository tourRepository,
    ITourCapacityService capacityService,
    IContentToursUnitOfWork unitOfWork,
    IContentToursOutboxWriter outbox,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<CreateTourPackageCommandHandler> logger)
    : ICommandHandler<CreateTourPackageCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        CreateTourPackageCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (currentUser.UserId is null)
            {
                return Result.Failure<Guid>(
                    new Error("Auth.Unauthorized", "Authentication is required."),
                    Outcome.Unauthorized);
            }

            var creatorId = currentUser.UserId.Value;

            // ── 1. IncludedTourIds shape: ≥2 distinct, no Empty ────────────────
            var rawIds = request.IncludedTourIds ?? Array.Empty<Guid>();
            var nonEmpty = rawIds.Where(id => id != Guid.Empty).ToList();
            var distinctIds = nonEmpty.Distinct().ToList();

            if (distinctIds.Count < 2)
            {
                return Result.Failure<Guid>(
                    new Error(
                        "TourPackage.InsufficientInclusions",
                        "A tour package must include at least 2 distinct tours."),
                    Outcome.Invalid);
            }

            if (distinctIds.Count != nonEmpty.Count)
            {
                return Result.Failure<Guid>(
                    new Error(
                        "TourPackage.DuplicateInclusions",
                        "IncludedTourIds must not contain duplicate values."),
                    Outcome.Invalid);
            }

            // ── 2. Bulk-load tours via the read-side repository surface ────────
            var tours = await tourRepository
                .GetAllAsync(
                    filter: t => distinctIds.Contains(t.Id),
                    ct: cancellationToken,
                    asNoTracking: true)
                .ConfigureAwait(false);

            // ── 3. Unknown tours ───────────────────────────────────────────────
            if (tours.Count != distinctIds.Count)
            {
                var missing = distinctIds.Except(tours.Select(t => t.Id)).ToList();
                logger.LogWarning(
                    "TourPackage create rejected: unknown tour ids {MissingIds}", missing);
                return Result.Failure<Guid>(
                    new Error(
                        "TourPackage.IncludesUnknownTour",
                        $"One or more included tours do not exist: {string.Join(", ", missing)}."),
                    Outcome.Invalid);
            }

            // ── 4. Soft-deleted tours ──────────────────────────────────────────
            if (tours.Any(t => t.IsDeleted))
            {
                return Result.Failure<Guid>(
                    new Error(
                        "TourPackage.IncludesDeletedTour",
                        "One or more included tours have been deleted."),
                    Outcome.Invalid);
            }

            // ── 5. Approval status (PDF: 422 UnprocessableEntity) ──────────────
            if (tours.Any(t => t.Status != TourStatus.Approved))
            {
                return Result.Failure<Guid>(
                    new Error(
                        "TourPackage.IncludesNonApprovedTour",
                        "Every included tour must be in the Approved status."),
                    Outcome.UnprocessableEntity);
            }

            // ── 6. Ownership: every tour owned by caller unless caller is Admin ─
            var isAdminTier = AppRoles.HighestPrivilegeLevel(currentUser.Roles)
                >= RolePrivilegeLevel.Admin;
            if (!isAdminTier && tours.Any(t => t.CreatedByUserId != creatorId))
            {
                return Result.Failure<Guid>(
                    new Error(
                        "TourPackage.NotOwnerOfAllTours",
                        "You do not own every included tour."),
                    Outcome.Forbidden);
            }

            // ── 7. Currency match across the package and all tours ─────────────
            if (string.IsNullOrWhiteSpace(request.Currency) || request.Currency.Length != 3)
            {
                return Result.Failure<Guid>(
                    new Error(
                        "TourPackage.CurrencyMismatch",
                        "Currency must be a 3-letter ISO code that matches every included tour."),
                    Outcome.Invalid);
            }

            var normalizedCurrency = request.Currency.ToUpperInvariant();
            if (tours.Any(t => !string.Equals(t.Currency, normalizedCurrency, StringComparison.OrdinalIgnoreCase)))
            {
                return Result.Failure<Guid>(
                    new Error(
                        "TourPackage.CurrencyMismatch",
                        "Package currency must match the currency of every included tour."),
                    Outcome.Invalid);
            }

            // ── 8. MaxParticipants <= min(MaxGroupSize) ────────────────────────
            if (request.MaxParticipants is { } cap)
            {
                var capLimit = tours.Min(t => t.MaxGroupSize);
                if (cap > capLimit)
                {
                    return Result.Failure<Guid>(
                        new Error(
                            "TourPackage.MaxParticipantsExceedsTour",
                            $"MaxParticipants ({cap}) exceeds the smallest included tour group size ({capLimit})."),
                        Outcome.Invalid);
                }
            }

            // ── 9. Validity window invariants ──────────────────────────────────
            if (request.ValidFrom.HasValue && request.ValidTo.HasValue
                && request.ValidFrom.Value >= request.ValidTo.Value)
            {
                return Result.Failure<Guid>(
                    new Error(
                        "TourPackage.InvalidValidityWindow",
                        "ValidFrom must be earlier than ValidTo."),
                    Outcome.Invalid);
            }

            if (request.ValidTo.HasValue && request.ValidTo.Value <= DateTime.UtcNow)
            {
                return Result.Failure<Guid>(
                    new Error(
                        "TourPackage.InvalidValidityWindow",
                        "ValidTo must be in the future."),
                    Outcome.Invalid);
            }

            // ── 10. Atomic capacity check (cross-module hook) ──────────────────
            var hasCapacity = await capacityService
                .AllHaveCapacityAsync(distinctIds, request.MaxParticipants, cancellationToken)
                .ConfigureAwait(false);
            if (!hasCapacity)
            {
                return Result.Failure<Guid>(
                    new Error(
                        "TourPackage.AtomicBookingFailed",
                        "One or more included tours could not reserve capacity for this package."),
                    Outcome.Conflict);
            }

            // ── 11. Build the entity ───────────────────────────────────────────
            var price = new Money(request.Price, normalizedCurrency);
            var package = TourPackageEntity.Create(
                name: request.Name,
                description: request.Description,
                price: price,
                currency: normalizedCurrency,
                maxParticipants: request.MaxParticipants,
                validFrom: request.ValidFrom,
                validTo: request.ValidTo,
                createdByUserId: creatorId,
                includedTourIds: distinctIds,
                inclusionDescriptions: request.Inclusions ?? Array.Empty<string>());

            await packageRepository.AddAsync(package, cancellationToken).ConfigureAwait(false);

            // ── 12. Outbox enqueue BEFORE save (atomic with the package row) ───
            outbox.Enqueue(new TourPackageCreatedIntegrationEvent(
                PackageId:       package.Id,
                CreatedByUserId: package.CreatedByUserId,
                Name:            package.Name,
                Price:           package.Price.Amount,
                Currency:        package.Currency,
                MaxParticipants: package.MaxParticipants,
                ValidFrom:       package.ValidFrom,
                ValidTo:         package.ValidTo,
                IncludedTourIds: distinctIds));

            // ── 13. Persist with concurrency-conflict mapping ──────────────────
            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex,
                    "Concurrency conflict while creating TourPackage by {UserId}", creatorId);
                return Result.Failure<Guid>(
                    new Error(
                        "TourPackage.ConcurrencyConflict",
                        "The package was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            // ── 14. Cache invalidation AFTER save success ─────────────────────
            await cache.RemoveByTagAsync(ContentToursCacheKeys.TagPackagesList, cancellationToken)
                .ConfigureAwait(false);
            foreach (var tourId in distinctIds)
            {
                await cache.RemoveByTagAsync(ContentToursCacheKeys.TagForTour(tourId), cancellationToken)
                    .ConfigureAwait(false);
            }

            logger.LogInformation(
                "TourPackage {PackageId} created (Tours={TourCount}, By={UserId})",
                package.Id, distinctIds.Count, creatorId);

            return Result.Success(package.Id);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<Guid>(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
