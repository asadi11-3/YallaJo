using Booking.Domain.Enums;
using Booking.Domain.Events;
using Booking.Domain.ValueObjects;
using YallaJo.SharedKernel.Domain.Entities;
using YallaJo.SharedKernel.Domain.Exceptions;

namespace Booking.Domain.Entities;

/// <summary>
/// Aggregate root representing a user's booking for a tour.
/// Created in <see cref="BookingStatus.AwaitingPayment"/> state and progresses through the
/// booking state machine (see <see cref="BookingStatus"/>) via methods added in TASK 5.
/// </summary>
public sealed class TourBooking : AuditableEntity, IAggregateRoot
{
    private readonly List<JoinRequest> _joinRequests = [];

    // EF Core
    private TourBooking() { }

    // === Identity / relationships ===
    public Guid UserId { get; private set; }
    public Guid TourId { get; private set; }
    public Guid ProviderId { get; private set; }
    public Guid AvailabilitySlotId { get; private set; }
    public int ParticipantCount { get; private set; } = 1;

    // === Reference / business key ===
    /// <summary>Format: <c>YJ-YYYYMMDD-XXXXXX</c> (Crockford base32). Unique across bookings.</summary>
    public string Reference { get; private set; } = string.Empty;

    // === Pricing (flat decimals; precision 19,4 enforced by EF config) ===
    public decimal Subtotal { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal LoyaltyAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    /// <summary>ISO 4217. Locked from tour's currency at Step 1; never re-evaluated.</summary>
    public string Currency { get; private set; } = string.Empty;

    /// <summary>Commission rate stamped at booking creation time (e.g., 0.10m == 10%). Provider-facing only.</summary>
    public decimal CommissionRate { get; private set; }
    public decimal CommissionAmount { get; private set; }

    /// <summary>JSON-serialized <c>IReadOnlyList&lt;BookingLineItem&gt;</c> for auditability.</summary>
    public string LineItemsJson { get; private set; } = "[]";

    /// <summary>JSON snapshot of the refund policy at booking creation time. Used by cancel flow regardless of live policy edits.</summary>
    public string RefundPolicySnapshot { get; private set; } = "{}";

    // === Guide / private tour ===
    /// <summary>Guide (TourGuide) who owns the AvailabilitySlot. FK into ContentTours.TourGuides.</summary>
    public Guid GuideId { get; private set; }

    /// <summary>If true, the tourist booked the entire slot exclusively (private tour pricing applies).</summary>
    public bool IsPrivate { get; private set; }

    /// <summary>Non-null when this booking was created via an approved JoinRequest on another booking.</summary>
    public Guid? JoinedFromBookingId { get; private set; }

    // === Booking mode ===
    /// <summary>If true: AwaitingPayment -> Confirmed on payment. If false: AwaitingPayment -> PendingConfirmation on payment.</summary>
    public bool IsInstantBooking { get; private set; }
    public DateTime PaymentExpiresAt { get; private set; }

    // === State machine ===
    public BookingStatus Status { get; private set; } = BookingStatus.AwaitingPayment;

    // === Lifecycle timestamps + sources (TASK 5 will mutate these via transition methods) ===
    public DateTime? ConfirmedAt { get; private set; }
    public ConfirmationSource? ConfirmationSource { get; private set; }

    public DateTime? RejectedAt { get; private set; }
    public string? RejectionReason { get; private set; }

    public DateTime? CancelledAt { get; private set; }
    public CancellationSource? CancellationSource { get; private set; }
    public string? CancellationReason { get; private set; }
    public decimal? RefundAmount { get; private set; }

    public DateTime? CompletedAt { get; private set; }
    public Guid? CompletedByUserId { get; private set; }

    // === Phase 3 (G4a): Dispute lifecycle ===
    public DateTime? DisputedAt { get; private set; }
    public Guid? DisputeOpenedByUserId { get; private set; }
    public string? DisputeReason { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public Guid? ResolvedByAdminId { get; private set; }
    public string? ResolutionNotes { get; private set; }

    // === Optional user input ===
    public string? SpecialRequests { get; private set; }

    // === Navigation ===
    public IReadOnlyCollection<JoinRequest> JoinRequests => _joinRequests.AsReadOnly();

    /// <summary>
    /// Factory that constructs a booking in <see cref="BookingStatus.AwaitingPayment"/> state
    /// and raises <see cref="TourBookingCreatedDomainEvent"/>.
    /// </summary>
    /// <remarks>
    /// Programming-error guards throw <see cref="BusinessRuleViolationException"/>; the calling handler is
    /// responsible for translating business errors into <c>Result.Failure</c> BEFORE invoking this factory.
    /// </remarks>
    public static TourBooking Create(
        Guid userId,
        Guid tourId,
        Guid providerId,
        Guid guideId,
        Guid availabilitySlotId,
        int participantCount,
        BookingPricing pricing,
        BookingReference reference,
        string refundPolicySnapshot,
        bool isInstantBooking,
        DateTime paymentExpiresAt,
        string lineItemsJson,
        bool isPrivate = false,
        Guid? joinedFromBookingId = null)
    {
        if (userId == Guid.Empty) throw new BusinessRuleViolationException("UserId must be provided.");
        if (tourId == Guid.Empty) throw new BusinessRuleViolationException("TourId must be provided.");
        if (providerId == Guid.Empty) throw new BusinessRuleViolationException("ProviderId must be provided.");
        if (guideId == Guid.Empty) throw new BusinessRuleViolationException("GuideId must be provided.");
        if (availabilitySlotId == Guid.Empty) throw new BusinessRuleViolationException("AvailabilitySlotId must be provided.");
        if (participantCount < 1) throw new BusinessRuleViolationException("ParticipantCount must be at least 1.");
        if (pricing.TotalAmount <= 0m) throw new BusinessRuleViolationException("TotalAmount must be positive.");
        if (string.IsNullOrWhiteSpace(pricing.Currency)) throw new BusinessRuleViolationException("Currency must be provided.");
        if (string.IsNullOrWhiteSpace(reference.Value)) throw new BusinessRuleViolationException("Reference must be provided.");
        if (string.IsNullOrWhiteSpace(refundPolicySnapshot)) throw new BusinessRuleViolationException("RefundPolicySnapshot must be provided.");
        if (paymentExpiresAt <= DateTime.UtcNow) throw new BusinessRuleViolationException("PaymentExpiresAt must be in the future.");

        var booking = new TourBooking
        {
            UserId = userId,
            TourId = tourId,
            ProviderId = providerId,
            GuideId = guideId,
            AvailabilitySlotId = availabilitySlotId,
            ParticipantCount = participantCount,
            IsPrivate = isPrivate,
            JoinedFromBookingId = joinedFromBookingId,
            Subtotal = pricing.Subtotal,
            DiscountAmount = pricing.DiscountAmount,
            LoyaltyAmount = pricing.LoyaltyAmount,
            TotalAmount = pricing.TotalAmount,
            Currency = pricing.Currency,
            CommissionRate = pricing.CommissionRate,
            CommissionAmount = pricing.CommissionAmount,
            LineItemsJson = lineItemsJson,
            Reference = reference.Value,
            RefundPolicySnapshot = refundPolicySnapshot,
            IsInstantBooking = isInstantBooking,
            PaymentExpiresAt = paymentExpiresAt,
            Status = BookingStatus.AwaitingPayment
        };

        booking.AddDomainEvent(new TourBookingCreatedDomainEvent(
            BookingId: booking.Id,
            UserId: userId,
            TourId: tourId,
            ProviderId: providerId,
            GuideId: guideId,
            AvailabilitySlotId: availabilitySlotId,
            ParticipantCount: participantCount,
            TotalAmount: pricing.TotalAmount,
            Currency: pricing.Currency,
            Reference: reference.Value,
            IsInstantBooking: isInstantBooking,
            IsPrivate: isPrivate));

        return booking;
    }

    /// <summary>
    /// Optional setter for <see cref="SpecialRequests"/> used by future "edit booking" flows.
    /// Trimmed; null when empty after trim.
    /// </summary>
    public void SetSpecialRequests(string? value)
    {
        SpecialRequests = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        MarkUpdated();
    }

    // === TASK 5: Lifecycle state-transition methods ===

    /// <summary>
    /// Moves AwaitingPayment -> PendingConfirmation. Called by the payment-completed inbox handler when
    /// the tour is NOT instant-bookable. No domain event is raised here; the eventual Confirmed transition
    /// (via <see cref="Confirm"/> or by the auto-accept service) is what downstream consumers care about.
    /// </summary>
    public void MoveToPendingConfirmation()
    {
        if (Status != BookingStatus.AwaitingPayment)
            throw new BusinessRuleViolationException($"Cannot move to PendingConfirmation from state {Status}.");
        if (IsInstantBooking)
            throw new BusinessRuleViolationException("Instant booking should not enter PendingConfirmation.");

        Status = BookingStatus.PendingConfirmation;
        MarkUpdated();
    }

    /// <summary>
    /// Confirms the booking. Valid sources:
    /// - <see cref="ConfirmationSource.PaymentWebhook"/> from AwaitingPayment for instant bookings
    /// - <see cref="ConfirmationSource.Manual"/> from PendingConfirmation when provider explicitly confirms
    /// - <see cref="ConfirmationSource.AutoAccept"/> from PendingConfirmation by the auto-accept background service
    /// Raises <see cref="TourBookingConfirmedDomainEvent"/>.
    /// </summary>
    public void Confirm(ConfirmationSource source)
    {
        if (Status != BookingStatus.AwaitingPayment && Status != BookingStatus.PendingConfirmation)
            throw new BusinessRuleViolationException($"Cannot confirm from state {Status}.");

        var now = DateTime.UtcNow;
        Status = BookingStatus.Confirmed;
        ConfirmedAt = now;
        ConfirmationSource = source;
        MarkUpdated();

        AddDomainEvent(new TourBookingConfirmedDomainEvent(
            BookingId: Id,
            UserId: UserId,
            TourId: TourId,
            ProviderId: ProviderId,
            AvailabilitySlotId: AvailabilitySlotId,
            ParticipantCount: ParticipantCount,
            ConfirmedAt: now,
            Source: source));
    }

    /// <summary>
    /// Provider rejects a PendingConfirmation booking. Reason is required (10-500 chars; the calling
    /// handler/validator enforces the precise range; the domain only guards the lower bound).
    /// Automatically issues a 100% refund (downstream Finance consumes the integration event).
    /// Raises <see cref="TourBookingRejectedDomainEvent"/>.
    /// </summary>
    public void Reject(string reason)
    {
        if (Status != BookingStatus.PendingConfirmation)
            throw new BusinessRuleViolationException($"Cannot reject from state {Status}.");
        if (string.IsNullOrWhiteSpace(reason) || reason.Length < 10)
            throw new BusinessRuleViolationException("Rejection reason must be at least 10 characters.");

        var now = DateTime.UtcNow;
        var refundAmount = TotalAmount; // provider rejection => automatic full refund
        Status = BookingStatus.Rejected;
        RejectedAt = now;
        RejectionReason = reason.Trim();
        RefundAmount = refundAmount;
        MarkUpdated();

        AddDomainEvent(new TourBookingRejectedDomainEvent(
            BookingId: Id,
            UserId: UserId,
            TourId: TourId,
            ProviderId: ProviderId,
            AvailabilitySlotId: AvailabilitySlotId,
            ParticipantCount: ParticipantCount,
            RejectedAt: now,
            Reason: RejectionReason,
            RefundAmount: refundAmount,
            Currency: Currency));
    }

    /// <summary>
    /// Cancels a booking (user, provider, admin, or force-majeure). Computes refund per the rules:
    /// - Provider-initiated OR force-majeure -> ALWAYS 100% regardless of policy
    /// - Otherwise -> uses <paramref name="refundPercentage"/> (handler computes via RefundPolicy snapshot)
    /// The cancelled booking's previous state is captured in the domain event so the capacity-restore
    /// handler knows whether to release LockedCount (was AwaitingPayment/PendingConfirmation) or
    /// BookedCount (was Confirmed).
    /// Raises <see cref="TourBookingCancelledDomainEvent"/>.
    /// </summary>
    public void Cancel(BookingCancellationContext ctx, decimal refundPercentage)
    {
        if (ctx is null) throw new BusinessRuleViolationException("Cancellation context is required.");
        if (Status is BookingStatus.Cancelled or BookingStatus.Rejected or BookingStatus.Completed)
            throw new BusinessRuleViolationException($"Cannot cancel from state {Status}.");

        if (ctx.Source == Enums.CancellationSource.Provider && (string.IsNullOrWhiteSpace(ctx.Reason) || ctx.Reason.Length < 10))
            throw new BusinessRuleViolationException("Provider cancellation reason must be at least 10 characters.");

        // Provider or force-majeure ALWAYS pays 100% back; otherwise the policy-driven percentage applies.
        var effectivePercentage = (ctx.ProviderInitiated || ctx.ForceMajeureOverride)
            ? 100m
            : Math.Clamp(refundPercentage, 0m, 100m);

        var refundAmount = Math.Round(TotalAmount * effectivePercentage / 100m, 2, MidpointRounding.ToEven);

        var previousStatus = Status;
        var now = DateTime.UtcNow;
        Status = BookingStatus.Cancelled;
        CancelledAt = now;
        CancellationSource = ctx.Source;
        CancellationReason = string.IsNullOrWhiteSpace(ctx.Reason) ? null : ctx.Reason.Trim();
        RefundAmount = refundAmount;
        MarkUpdated();

        AddDomainEvent(new TourBookingCancelledDomainEvent(
            BookingId: Id,
            UserId: UserId,
            TourId: TourId,
            ProviderId: ProviderId,
            AvailabilitySlotId: AvailabilitySlotId,
            ParticipantCount: ParticipantCount,
            PreviousStatus: previousStatus,
            CancelledAt: now,
            Source: ctx.Source,
            Reason: CancellationReason,
            RefundAmount: refundAmount,
            Currency: Currency));
    }

    /// <summary>
    /// Marks a Confirmed booking as Completed. The caller (provider/admin) is captured for audit.
    /// Raises <see cref="TourBookingCompletedDomainEvent"/>.
    /// </summary>
    public void Complete(Guid completedByUserId)
    {
        if (Status != BookingStatus.Confirmed)
            throw new BusinessRuleViolationException($"Cannot complete from state {Status}.");
        if (completedByUserId == Guid.Empty)
            throw new BusinessRuleViolationException("CompletedByUserId must be provided.");

        var now = DateTime.UtcNow;
        Status = BookingStatus.Completed;
        CompletedAt = now;
        CompletedByUserId = completedByUserId;
        MarkUpdated();

        AddDomainEvent(new TourBookingCompletedDomainEvent(
            BookingId: Id,
            UserId: UserId,
            TourId: TourId,
            ProviderId: ProviderId,
            CompletedAt: now,
            CompletedByUserId: completedByUserId));
    }

    /// <summary>
    /// User opens a dispute on a Completed booking within the 48-hour post-completion window.
    /// Spec §G4a: Completed→Disputed; only the booking owner may open. Reason 10-2000 chars.
    /// Raises <see cref="TourBookingDisputedDomainEvent"/>.
    /// </summary>
    public void OpenDispute(Guid openedByUserId, string reason)
    {
        if (openedByUserId == Guid.Empty)
            throw new BusinessRuleViolationException("OpenedByUserId must be provided.");
        if (openedByUserId != UserId)
            throw new BusinessRuleViolationException("Only the booking owner may open a dispute.");
        if (Status != BookingStatus.Completed)
            throw new BusinessRuleViolationException($"Cannot open dispute from state {Status}. Must be Completed.");
        if (CompletedAt is null)
            throw new BusinessRuleViolationException("Completed booking has no CompletedAt timestamp.");
        if (DateTime.UtcNow > CompletedAt.Value.AddHours(48))
            throw new BusinessRuleViolationException("Dispute window has closed (48 hours after completion).");
        if (string.IsNullOrWhiteSpace(reason) || reason.Length < 10)
            throw new BusinessRuleViolationException("Dispute reason must be at least 10 characters.");
        if (reason.Length > 2000)
            throw new BusinessRuleViolationException("Dispute reason must be at most 2000 characters.");

        var now = DateTime.UtcNow;
        Status = BookingStatus.Disputed;
        DisputedAt = now;
        DisputeOpenedByUserId = openedByUserId;
        DisputeReason = reason.Trim();
        MarkUpdated();

        AddDomainEvent(new TourBookingDisputedDomainEvent(
            BookingId: Id,
            UserId: UserId,
            TourId: TourId,
            ProviderId: ProviderId,
            DisputedAt: now,
            Reason: DisputeReason));
    }

    /// <summary>
    /// Admin resolves a Disputed booking. Terminal state for the dispute lifecycle.
    /// Resolution notes 10-2000 chars.
    /// Raises <see cref="TourBookingDisputeResolvedDomainEvent"/>.
    /// </summary>
    public void ResolveDispute(Guid resolvedByAdminId, string resolutionNotes)
    {
        if (resolvedByAdminId == Guid.Empty)
            throw new BusinessRuleViolationException("ResolvedByAdminId must be provided.");
        if (Status != BookingStatus.Disputed)
            throw new BusinessRuleViolationException($"Cannot resolve dispute from state {Status}. Must be Disputed.");
        if (string.IsNullOrWhiteSpace(resolutionNotes) || resolutionNotes.Length < 10)
            throw new BusinessRuleViolationException("Resolution notes must be at least 10 characters.");
        if (resolutionNotes.Length > 2000)
            throw new BusinessRuleViolationException("Resolution notes must be at most 2000 characters.");

        var now = DateTime.UtcNow;
        Status = BookingStatus.Resolved;
        ResolvedAt = now;
        ResolvedByAdminId = resolvedByAdminId;
        ResolutionNotes = resolutionNotes.Trim();
        MarkUpdated();

        AddDomainEvent(new TourBookingDisputeResolvedDomainEvent(
            BookingId: Id,
            UserId: UserId,
            TourId: TourId,
            ProviderId: ProviderId,
            ResolvedByAdminId: resolvedByAdminId,
            ResolvedAt: now,
            ResolutionNotes: ResolutionNotes));
    }

    /// <summary>
    /// Called by the BookingAutoExpireService when the AwaitingPayment window elapses without payment.
    /// Cancels the booking with no refund and raises <see cref="TourBookingPaymentExpiredDomainEvent"/>
    /// so the capacity-restore handler can release the slot lock.
    /// </summary>
    public void MoveToAwaitingPaymentExpired()
    {
        if (Status != BookingStatus.AwaitingPayment)
            throw new BusinessRuleViolationException($"Cannot expire payment from state {Status}.");

        var now = DateTime.UtcNow;
        Status = BookingStatus.Cancelled;
        CancelledAt = now;
        CancellationSource = Enums.CancellationSource.System;
        CancellationReason = "Payment expired (no payment received within the AwaitingPayment window).";
        RefundAmount = 0m;
        MarkUpdated();

        AddDomainEvent(new TourBookingPaymentExpiredDomainEvent(
            BookingId: Id,
            UserId: UserId,
            TourId: TourId,
            AvailabilitySlotId: AvailabilitySlotId,
            ParticipantCount: ParticipantCount,
            CancelledAt: now));
    }
}
