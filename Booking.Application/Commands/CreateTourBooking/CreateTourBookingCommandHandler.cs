using System.Text.Json;
using Booking.Application.Interfaces;
using Booking.Domain.Entities;
using Booking.Domain.Enums;
using Booking.Domain.Repositories;
using Booking.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Domain.Exceptions;

namespace Booking.Application.Commands.CreateTourBooking;

/// <summary>
/// Five-step tour booking engine: validate → lock slot → price → create aggregate → emit event.
/// </summary>
public sealed class CreateTourBookingCommandHandler(
    ITourBookingRepository tourBookingRepository,
    IAvailabilitySlotRepository availabilitySlotRepository,
    ISlotLockRepository slotLockRepository,
    IBookingTourSnapshotReader tourSnapshotReader,
    IBookingProviderSnapshotReader providerSnapshotReader,
    IBookingPricingSnapshotReader pricingSnapshotReader,
    IBookingReferenceGenerator referenceGenerator,
    IBookingCommissionLookup commissionLookup,
    IDiscountEvaluator discountEvaluator,
    IBookingUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<CreateTourBookingCommandHandler> logger)
    : ICommandHandler<CreateTourBookingCommand, CreateTourBookingResult>
{
    // Spec: AwaitingPayment booking expires after 10 minutes if no payment is received.
    private static readonly TimeSpan PaymentWindow = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan SlotLockTtl = TimeSpan.FromMinutes(10);

    // Spec: minimum lead time before the slot start. Booking is rejected if start is closer.
    private static readonly TimeSpan MinimumLeadTime = TimeSpan.FromHours(2);

    private const int MaxConcurrentAwaitingPayment = 3;
    private const decimal PlatformMinimumJod = 5m;
    private const string PaymentTokenPlaceholder = "PENDING_FINANCE_INTEGRATION";

    private static readonly JsonSerializerOptions LineItemsJsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<Result<CreateTourBookingResult>> Handle(
        CreateTourBookingCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // ----- Auth check (RuleERR-004: explicit Outcome.Unauthorized) -----
            if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            {
                logger.LogWarning("CreateTourBooking rejected: unauthenticated caller.");
                return Result.Failure<CreateTourBookingResult>(
                    new Error("TourBooking.Unauthorized", "Authentication is required to create a booking."),
                    Outcome.Unauthorized);
            }

            var userId = currentUser.UserId.Value;
            var participantCount = request.ParticipantBreakdown.Total;

            // =================================================================
            // Step 1 — Validate availability
            // =================================================================
            var tourSnapshot = await tourSnapshotReader
                .GetByIdAsync(request.TourId, cancellationToken)
                .ConfigureAwait(false);

            if (tourSnapshot is null || !tourSnapshot.IsActive || !tourSnapshot.IsApproved)
            {
                logger.LogWarning("CreateTourBooking: tour {TourId} not found / inactive.", request.TourId);
                return Result.Failure<CreateTourBookingResult>(
                    new Error("Tour.NotFound", "Tour is unavailable for booking."),
                    Outcome.NotFound);
            }

            var providerSnapshot = await providerSnapshotReader
                .GetByIdAsync(tourSnapshot.ProviderId, cancellationToken)
                .ConfigureAwait(false);

            if (providerSnapshot is null || providerSnapshot.Status == BookingProviderStatus.Suspended)
            {
                logger.LogWarning(
                    "CreateTourBooking: provider {ProviderId} suspended or missing.",
                    tourSnapshot.ProviderId);
                return Result.Failure<CreateTourBookingResult>(
                    new Error("Booking.ProviderSuspended", "Provider is currently suspended."),
                    Outcome.Conflict);
            }

            var slot = await availabilitySlotRepository
                .GetByIdWithLockAsync(request.AvailabilitySlotId, cancellationToken)
                .ConfigureAwait(false);

            if (slot is null || !slot.IsActive || slot.TourId != request.TourId)
            {
                logger.LogWarning(
                    "CreateTourBooking: slot {SlotId} missing/inactive or mismatched tour {TourId}.",
                    request.AvailabilitySlotId,
                    request.TourId);
                return Result.Failure<CreateTourBookingResult>(
                    new Error("AvailabilitySlot.NotFound", "Availability slot is unavailable."),
                    Outcome.NotFound);
            }

            if (slot.AvailableCount < participantCount)
            {
                logger.LogInformation(
                    "CreateTourBooking: slot {SlotId} capacity {Available} < requested {Requested}.",
                    slot.Id,
                    slot.AvailableCount,
                    participantCount);
                return Result.Failure<CreateTourBookingResult>(
                    new Error(
                        "AvailabilitySlot.CapacityExceeded",
                        "Not enough seats available for the requested participant count."),
                    Outcome.Conflict);
            }

            var slotStart = ComposeUtc(slot.Date, slot.StartTime);
            var now = DateTime.UtcNow;
            var leadTime = slotStart - now;

            if (leadTime < MinimumLeadTime)
            {
                logger.LogInformation(
                    "CreateTourBooking: slot {SlotId} starts in {LeadHours}h, below minimum.",
                    slot.Id,
                    leadTime.TotalHours);
                return Result.Failure<CreateTourBookingResult>(
                    new Error(
                        "TourBooking.TooEarly",
                        $"Bookings must be created at least {MinimumLeadTime.TotalHours:F0} hours before the tour starts."),
                    Outcome.Invalid);
            }

            var duplicateForDate = await tourBookingRepository
                .HasActiveBookingForTourOnDateAsync(userId, request.TourId, slot.Date, cancellationToken)
                .ConfigureAwait(false);

            if (duplicateForDate)
            {
                logger.LogInformation(
                    "CreateTourBooking: user {UserId} already has a booking for tour {TourId} on {Date}.",
                    userId,
                    request.TourId,
                    slot.Date);
                return Result.Failure<CreateTourBookingResult>(
                    new Error(
                        "TourBooking.DuplicateForDate",
                        "You already have an active booking for this tour on the same date."),
                    Outcome.Conflict);
            }

            var concurrentUnpaid = await tourBookingRepository
                .CountActiveAwaitingPaymentByUserAsync(userId, cancellationToken)
                .ConfigureAwait(false);

            if (concurrentUnpaid >= MaxConcurrentAwaitingPayment)
            {
                logger.LogInformation(
                    "CreateTourBooking: user {UserId} has {Count} pending bookings (limit {Limit}).",
                    userId,
                    concurrentUnpaid,
                    MaxConcurrentAwaitingPayment);
                return Result.Failure<CreateTourBookingResult>(
                    new Error(
                        "TourBooking.ConcurrentLimit",
                        $"You cannot have more than {MaxConcurrentAwaitingPayment} unpaid bookings at once."),
                    Outcome.Conflict);
            }

            // =================================================================
            // Step 2 — Lock the slot (in-aggregate; row-version backed)
            // =================================================================
            try
            {
                slot.Lock(participantCount);
            }
            catch (BusinessRuleViolationException ex)
            {
                // Defensive: AvailableCount race lost to a parallel handler between the check and Lock().
                logger.LogWarning(ex, "CreateTourBooking: slot.Lock guard fired for slot {SlotId}.", slot.Id);
                return Result.Failure<CreateTourBookingResult>(
                    new Error("AvailabilitySlot.CapacityExceeded", ex.Message),
                    Outcome.Conflict);
            }

            // =================================================================
            // Step 3 — Pricing (subtotal → discount → loyalty → commission)
            // =================================================================
            var currency = string.IsNullOrWhiteSpace(tourSnapshot.Currency)
                ? "JOD"
                : tourSnapshot.Currency.ToUpperInvariant();

            if (!IsSupportedCurrency(currency))
            {
                logger.LogWarning("CreateTourBooking: unsupported currency {Currency} on tour {TourId}.", currency, request.TourId);
                return Result.Failure<CreateTourBookingResult>(
                    new Error("TourBooking.UnsupportedCurrency", $"Currency '{currency}' is not supported."),
                    Outcome.Invalid);
            }

            var lineItems = new List<BookingLineItem>();
            foreach (var (tier, count) in request.ParticipantBreakdown.NonEmptyTiers())
            {
                var unitPrice = await ResolveUnitPriceAsync(
                    request.TourId,
                    tier,
                    tourSnapshot.BasePrice,
                    currency,
                    cancellationToken).ConfigureAwait(false);

                if (unitPrice is null)
                {
                    logger.LogWarning(
                        "CreateTourBooking: missing pricing tier {Tier} for tour {TourId}.",
                        tier,
                        request.TourId);
                    return Result.Failure<CreateTourBookingResult>(
                        new Error(
                            "TourBooking.InvalidPricingConfiguration",
                            $"Pricing tier '{tier}' is not configured for this tour."),
                        Outcome.Invalid);
                }

                lineItems.Add(new BookingLineItem(tier, count, unitPrice.Value, currency));
            }

            var subtotal = lineItems.Sum(li => li.LineTotal);

            var discount = await discountEvaluator
                .EvaluateAsync(
                    new DiscountEvaluationContext(
                        userId,
                        request.TourId,
                        tourSnapshot.ProviderId,
                        subtotal,
                        currency,
                        request.PromoCode),
                    cancellationToken)
                .ConfigureAwait(false);

            var afterDiscount = subtotal - discount.AppliedAmount;

            // Loyalty redemption deferred to Finance sprint — stamp 0 for now.
            const decimal loyaltyAmount = 0m;
            var totalAmount = afterDiscount - loyaltyAmount;

            if (currency == "JOD" && totalAmount < PlatformMinimumJod)
            {
                logger.LogInformation(
                    "CreateTourBooking: total {Total} JOD below platform minimum {Minimum}.",
                    totalAmount,
                    PlatformMinimumJod);
                return Result.Failure<CreateTourBookingResult>(
                    new Error(
                        "TourBooking.BelowPlatformMinimum",
                        $"Booking total must be at least {PlatformMinimumJod} JOD."),
                    Outcome.Invalid);
            }

            if (totalAmount <= 0m)
            {
                logger.LogInformation("CreateTourBooking: computed non-positive total {Total}.", totalAmount);
                return Result.Failure<CreateTourBookingResult>(
                    new Error("TourBooking.InvalidPricing", "Final price must be a positive amount."),
                    Outcome.Invalid);
            }

            var commission = await commissionLookup
                .GetForTourAsync(request.TourId, cancellationToken)
                .ConfigureAwait(false);

            var commissionAmount = Math.Round(
                totalAmount * commission.Rate,
                2,
                MidpointRounding.ToEven);

            var pricing = new BookingPricing(
                Subtotal: subtotal,
                DiscountAmount: discount.AppliedAmount,
                LoyaltyAmount: loyaltyAmount,
                TotalAmount: totalAmount,
                CommissionRate: commission.Rate,
                CommissionAmount: commissionAmount,
                Currency: currency,
                LineItems: lineItems);

            // =================================================================
            // Step 4 — Create aggregate + slot lock; one atomic SaveChanges
            // =================================================================
            var reference = await referenceGenerator
                .GenerateAsync(cancellationToken)
                .ConfigureAwait(false);

            var lineItemsJson = JsonSerializer.Serialize(lineItems, LineItemsJsonOptions);
            var refundPolicySnapshot = string.IsNullOrWhiteSpace(tourSnapshot.RefundPolicySnapshotJson)
                ? "{}"
                : tourSnapshot.RefundPolicySnapshotJson;

            TourBooking booking;
            try
            {
                booking = TourBooking.Create(
                    userId: userId,
                    tourId: request.TourId,
                    providerId: tourSnapshot.ProviderId,
                    availabilitySlotId: slot.Id,
                    participantCount: participantCount,
                    pricing: pricing,
                    reference: reference,
                    refundPolicySnapshot: refundPolicySnapshot,
                    isInstantBooking: tourSnapshot.IsInstantBooking,
                    paymentExpiresAt: now.Add(PaymentWindow),
                    lineItemsJson: lineItemsJson);
            }
            catch (BusinessRuleViolationException ex)
            {
                logger.LogWarning(ex, "CreateTourBooking: aggregate creation invariant violated.");
                return Result.Failure<CreateTourBookingResult>(
                    new Error("TourBooking.InvalidState", ex.Message),
                    Outcome.Invalid);
            }

            if (!string.IsNullOrWhiteSpace(request.SpecialRequests))
            {
                booking.SetSpecialRequests(request.SpecialRequests);
            }

            SlotLock slotLock;
            try
            {
                slotLock = SlotLock.Create(
                    userId: userId,
                    availabilitySlotId: slot.Id,
                    participantCount: participantCount,
                    ttl: SlotLockTtl,
                    bookingId: booking.Id);
            }
            catch (BusinessRuleViolationException ex)
            {
                logger.LogWarning(ex, "CreateTourBooking: slot lock invariant violated.");
                return Result.Failure<CreateTourBookingResult>(
                    new Error("AvailabilitySlot.InvalidLock", ex.Message),
                    Outcome.Invalid);
            }

            await tourBookingRepository.AddAsync(booking, cancellationToken).ConfigureAwait(false);
            await slotLockRepository.AddAsync(slotLock, cancellationToken).ConfigureAwait(false);

            // =================================================================
            // Step 5 — Persist atomically
            // =================================================================
            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(
                    ex,
                    "CreateTourBooking: RowVersion conflict on slot {SlotId}.",
                    slot.Id);
                return Result.Failure<CreateTourBookingResult>(
                    new Error(
                        "AvailabilitySlot.CapacityConflict",
                        "Slot was modified by another booking. Please try again."),
                    Outcome.Conflict);
            }

            // Cache invalidation AFTER successful save (ERR-010: most-specific tags first).
            await cache
                .RemoveByTagAsync($"bookings:user:{userId}", cancellationToken)
                .ConfigureAwait(false);
            await cache
                .RemoveByTagAsync($"availability:tour:{request.TourId}:date:{slot.Date:yyyy-MM-dd}", cancellationToken)
                .ConfigureAwait(false);
            await cache
                .RemoveByTagAsync($"availability:tour:{request.TourId}", cancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation(
                "TourBooking created. Id={BookingId} Reference={Reference} User={UserId} Tour={TourId} Slot={SlotId} Total={Total} {Currency} Participants={Participants}",
                booking.Id,
                booking.Reference,
                userId,
                booking.TourId,
                booking.AvailabilitySlotId,
                booking.TotalAmount,
                booking.Currency,
                booking.ParticipantCount);

            var resultDto = new CreateTourBookingResult(
                Id: booking.Id,
                Reference: booking.Reference,
                Status: booking.Status,
                TourId: booking.TourId,
                AvailabilitySlotId: booking.AvailabilitySlotId,
                ParticipantCount: booking.ParticipantCount,
                Pricing: new BookingPricingDto(
                    Subtotal: pricing.Subtotal,
                    DiscountAmount: pricing.DiscountAmount,
                    LoyaltyAmount: pricing.LoyaltyAmount,
                    TotalAmount: pricing.TotalAmount,
                    Currency: pricing.Currency,
                    LineItems: lineItems
                        .Select(li => new BookingLineItemDto(li.TierType, li.Count, li.UnitPrice))
                        .ToList()),
                PaymentExpiresAt: booking.PaymentExpiresAt,
                PaymentToken: PaymentTokenPlaceholder);

            return Result.Created(resultDto);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation("CreateTourBooking cancelled by client.");
            return Result.Failure<CreateTourBookingResult>(
                new Error("Request.Cancelled", "The request was cancelled by the client."),
                Outcome.Canceled);
        }
    }

    private async Task<decimal?> ResolveUnitPriceAsync(
        Guid tourId,
        TierType tier,
        decimal basePrice,
        string currency,
        CancellationToken cancellationToken)
    {
        var tierSnapshot = await pricingSnapshotReader
            .GetByTourAndTypeAsync(tourId, tier, cancellationToken)
            .ConfigureAwait(false);

        if (tierSnapshot is not null)
        {
            if (!string.Equals(tierSnapshot.Currency, currency, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning(
                    "CreateTourBooking: tier {Tier} currency {TierCurrency} mismatches tour {TourCurrency}.",
                    tier,
                    tierSnapshot.Currency,
                    currency);
                return null;
            }

            return tierSnapshot.Price;
        }

        // Adult tier always falls back to BasePrice; other tiers must be configured explicitly
        // when present in the breakdown (Children/Infants/Seniors have different rates).
        return tier == TierType.Adult ? basePrice : null;
    }

    private static bool IsSupportedCurrency(string currency)
        => currency is "JOD" or "USD" or "EUR";

    private static DateTime ComposeUtc(DateOnly date, TimeOnly time)
        => DateTime.SpecifyKind(date.ToDateTime(time), DateTimeKind.Utc);
}
