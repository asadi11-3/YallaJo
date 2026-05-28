using Booking.Domain.Entities;
using Booking.Domain.Enums;
using Booking.Domain.Events;
using Booking.Domain.ValueObjects;
using FluentAssertions;
using YallaJo.SharedKernel.Domain.Exceptions;
using YallaJo.Tests.Shared;

namespace Booking.Tests.Unit.Domain;

/// <summary>
/// Unit tests for <see cref="TourBooking"/> aggregate root.
/// Covers the Create factory plus all 6 state transitions delivered in Mohammad's TASK 5.
/// </summary>
public sealed class TourBookingTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TourId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ProviderId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid SlotId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid CompletedBy = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid GuideId = Guid.Parse("66666666-6666-6666-6666-666666666666");

    private static BookingPricing DefaultPricing(decimal totalAmount = 90m, string currency = "JOD") =>
        new(
            Subtotal: totalAmount,
            DiscountAmount: 0m,
            LoyaltyAmount: 0m,
            TotalAmount: totalAmount,
            CommissionRate: 0.10m,
            CommissionAmount: Math.Round(totalAmount * 0.10m, 2, MidpointRounding.ToEven),
            Currency: currency,
            LineItems: new List<BookingLineItem>
            {
                new(TierType.Adult, 2, 45m, currency)
            });

    private static TourBooking CreateBooking(
        bool instant = true,
        DateTime? paymentExpiresAt = null,
        decimal total = 90m,
        string currency = "JOD")
    {
        var pricing = DefaultPricing(total, currency);
        var reference = BookingReference.Compose(new DateOnly(2026, 7, 1), "A7X3K9");
        return TourBooking.Create(
            userId: UserId,
            tourId: TourId,
            providerId: ProviderId,
            guideId: GuideId,
            availabilitySlotId: SlotId,
            participantCount: 2,
            pricing: pricing,
            reference: reference,
            refundPolicySnapshot: "{}",
            isInstantBooking: instant,
            paymentExpiresAt: paymentExpiresAt ?? DateTime.UtcNow.AddMinutes(10),
            lineItemsJson: "[]");
    }

    // ===== Create factory =====

    [Fact]
    public void Create_with_valid_inputs_yields_booking_in_AwaitingPayment_state()
    {
        var booking = CreateBooking();

        booking.Status.Should().Be(BookingStatus.AwaitingPayment);
        booking.UserId.Should().Be(UserId);
        booking.TourId.Should().Be(TourId);
        booking.ProviderId.Should().Be(ProviderId);
        booking.AvailabilitySlotId.Should().Be(SlotId);
        booking.ParticipantCount.Should().Be(2);
        booking.TotalAmount.Should().Be(90m);
        booking.Currency.Should().Be("JOD");
        booking.Reference.Should().Be("YJ-20260701-A7X3K9");
        booking.IsInstantBooking.Should().BeTrue();
    }

    [Fact]
    public void Create_raises_TourBookingCreatedDomainEvent_with_full_payload()
    {
        var booking = CreateBooking();

        var evt = booking.ShouldContainDomainEvent<TourBookingCreatedDomainEvent>();
        evt.BookingId.Should().Be(booking.Id);
        evt.UserId.Should().Be(UserId);
        evt.TourId.Should().Be(TourId);
        evt.ProviderId.Should().Be(ProviderId);
        evt.AvailabilitySlotId.Should().Be(SlotId);
        evt.ParticipantCount.Should().Be(2);
        evt.TotalAmount.Should().Be(90m);
        evt.Currency.Should().Be("JOD");
        evt.Reference.Should().Be("YJ-20260701-A7X3K9");
        evt.IsInstantBooking.Should().BeTrue();
    }

    [Theory]
    [InlineData(0, "userId")]   // empty userId
    [InlineData(-1, "participantCount")]  // negative participant count not allowed (the factory uses >=1)
    public void Create_throws_when_required_input_is_invalid(int kind, string _expectedField)
    {
        Action act = kind switch
        {
            0 => () => TourBooking.Create(
                userId: Guid.Empty,
                tourId: TourId,
                providerId: ProviderId,
                guideId: GuideId,
                availabilitySlotId: SlotId,
                participantCount: 2,
                pricing: DefaultPricing(),
                reference: BookingReference.Compose(new DateOnly(2026, 1, 1), "A7X3K9"),
                refundPolicySnapshot: "{}",
                isInstantBooking: true,
                paymentExpiresAt: DateTime.UtcNow.AddMinutes(10),
                lineItemsJson: "[]"),
            -1 => () => TourBooking.Create(
                userId: UserId,
                tourId: TourId,
                providerId: ProviderId,
                guideId: GuideId,
                availabilitySlotId: SlotId,
                participantCount: 0,
                pricing: DefaultPricing(),
                reference: BookingReference.Compose(new DateOnly(2026, 1, 1), "A7X3K9"),
                refundPolicySnapshot: "{}",
                isInstantBooking: true,
                paymentExpiresAt: DateTime.UtcNow.AddMinutes(10),
                lineItemsJson: "[]"),
            _ => () => { }
        };

        act.Should().Throw<BusinessRuleViolationException>();
    }

    [Fact]
    public void Create_throws_when_PaymentExpiresAt_is_in_the_past()
    {
        var act = () => CreateBooking(paymentExpiresAt: DateTime.UtcNow.AddMinutes(-1));

        act.Should().Throw<BusinessRuleViolationException>();
    }

    // ===== Confirm =====

    [Fact]
    public void Confirm_from_AwaitingPayment_transitions_to_Confirmed_and_raises_event()
    {
        var booking = CreateBooking(instant: true);

        booking.Confirm(ConfirmationSource.PaymentWebhook);

        booking.Status.Should().Be(BookingStatus.Confirmed);
        booking.ConfirmedAt.Should().NotBeNull();
        booking.ConfirmationSource.Should().Be(ConfirmationSource.PaymentWebhook);
        var evt = booking.ShouldContainDomainEvent<TourBookingConfirmedDomainEvent>();
        evt.Source.Should().Be(ConfirmationSource.PaymentWebhook);
    }

    [Fact]
    public void Confirm_from_PendingConfirmation_transitions_to_Confirmed()
    {
        var booking = CreateBooking(instant: false);
        booking.MoveToPendingConfirmation();

        booking.Confirm(ConfirmationSource.Manual);

        booking.Status.Should().Be(BookingStatus.Confirmed);
        booking.ConfirmationSource.Should().Be(ConfirmationSource.Manual);
    }

    [Fact]
    public void Confirm_from_invalid_state_throws()
    {
        var booking = CreateBooking();
        booking.Confirm(ConfirmationSource.PaymentWebhook); // -> Confirmed

        var act = () => booking.Confirm(ConfirmationSource.PaymentWebhook);

        act.Should().Throw<BusinessRuleViolationException>();
    }

    // ===== MoveToPendingConfirmation =====

    [Fact]
    public void MoveToPendingConfirmation_from_AwaitingPayment_non_instant_succeeds()
    {
        var booking = CreateBooking(instant: false);

        booking.MoveToPendingConfirmation();

        booking.Status.Should().Be(BookingStatus.PendingConfirmation);
    }

    [Fact]
    public void MoveToPendingConfirmation_throws_when_booking_is_instant()
    {
        var booking = CreateBooking(instant: true);

        var act = () => booking.MoveToPendingConfirmation();

        act.Should().Throw<BusinessRuleViolationException>();
    }

    // ===== Reject =====

    [Fact]
    public void Reject_from_PendingConfirmation_with_valid_reason_succeeds_with_full_refund()
    {
        var booking = CreateBooking(instant: false);
        booking.MoveToPendingConfirmation();

        booking.Reject("Provider unavailable due to family emergency.");

        booking.Status.Should().Be(BookingStatus.Rejected);
        booking.RejectionReason.Should().Be("Provider unavailable due to family emergency.");
        booking.RefundAmount.Should().Be(90m);
        var evt = booking.ShouldContainDomainEvent<TourBookingRejectedDomainEvent>();
        evt.RefundAmount.Should().Be(90m);
        evt.Currency.Should().Be("JOD");
        evt.AvailabilitySlotId.Should().Be(SlotId);
        evt.ParticipantCount.Should().Be(2);
    }

    [Fact]
    public void Reject_from_non_PendingConfirmation_state_throws()
    {
        var booking = CreateBooking();

        var act = () => booking.Reject("This is a long enough reason.");

        act.Should().Throw<BusinessRuleViolationException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("too short")]
    public void Reject_with_invalid_reason_throws(string reason)
    {
        var booking = CreateBooking(instant: false);
        booking.MoveToPendingConfirmation();

        var act = () => booking.Reject(reason);

        act.Should().Throw<BusinessRuleViolationException>();
    }

    // ===== Cancel =====

    [Fact]
    public void Cancel_by_provider_always_pays_100_percent_regardless_of_percentage()
    {
        var booking = CreateBooking();
        booking.Confirm(ConfirmationSource.PaymentWebhook);
        var ctx = new BookingCancellationContext(
            Source: CancellationSource.Provider,
            Reason: "Schedule conflict on tour day.",
            ProviderInitiated: true,
            ForceMajeureOverride: false);

        booking.Cancel(ctx, refundPercentage: 25m);

        booking.Status.Should().Be(BookingStatus.Cancelled);
        booking.RefundAmount.Should().Be(90m);
        var evt = booking.ShouldContainDomainEvent<TourBookingCancelledDomainEvent>();
        evt.PreviousStatus.Should().Be(BookingStatus.Confirmed);
        evt.RefundAmount.Should().Be(90m);
        evt.Source.Should().Be(CancellationSource.Provider);
    }

    [Fact]
    public void Cancel_by_user_applies_clamped_percentage()
    {
        var booking = CreateBooking();
        var ctx = new BookingCancellationContext(
            Source: CancellationSource.User,
            Reason: "Changed plans",
            ProviderInitiated: false,
            ForceMajeureOverride: false);

        booking.Cancel(ctx, refundPercentage: 50m);

        booking.RefundAmount.Should().Be(45m); // 90 * 0.5 (banker's rounding to 2 dp)
        booking.CancellationSource.Should().Be(CancellationSource.User);
    }

    [Fact]
    public void Cancel_force_majeure_always_pays_100_percent()
    {
        var booking = CreateBooking();
        booking.Confirm(ConfirmationSource.PaymentWebhook);
        var ctx = new BookingCancellationContext(
            Source: CancellationSource.Admin,
            Reason: "Hurricane evacuation in Petra region.",
            ProviderInitiated: false,
            ForceMajeureOverride: true);

        booking.Cancel(ctx, refundPercentage: 0m);

        booking.RefundAmount.Should().Be(90m);
    }

    [Fact]
    public void Cancel_clamps_percentage_above_100_to_100()
    {
        var booking = CreateBooking();
        var ctx = new BookingCancellationContext(CancellationSource.User, null, false, false);

        booking.Cancel(ctx, refundPercentage: 250m);

        booking.RefundAmount.Should().Be(90m);
    }

    [Fact]
    public void Cancel_clamps_percentage_below_zero_to_zero()
    {
        var booking = CreateBooking();
        var ctx = new BookingCancellationContext(CancellationSource.User, null, false, false);

        booking.Cancel(ctx, refundPercentage: -10m);

        booking.RefundAmount.Should().Be(0m);
    }

    [Fact]
    public void Cancel_throws_when_provider_reason_is_too_short()
    {
        var booking = CreateBooking();
        var ctx = new BookingCancellationContext(
            Source: CancellationSource.Provider,
            Reason: "nope",
            ProviderInitiated: true,
            ForceMajeureOverride: false);

        var act = () => booking.Cancel(ctx, 100m);

        act.Should().Throw<BusinessRuleViolationException>();
    }

    [Fact]
    public void Cancel_from_already_cancelled_state_throws()
    {
        var booking = CreateBooking();
        var ctx = new BookingCancellationContext(CancellationSource.User, null, false, false);
        booking.Cancel(ctx, 100m);

        var act = () => booking.Cancel(ctx, 100m);

        act.Should().Throw<BusinessRuleViolationException>();
    }

    [Fact]
    public void Cancel_captures_previous_status_in_event_for_capacity_restore_handler()
    {
        var booking = CreateBooking(instant: false);
        booking.MoveToPendingConfirmation();
        var ctx = new BookingCancellationContext(CancellationSource.User, null, false, false);

        booking.Cancel(ctx, 50m);

        var evt = booking.ShouldContainDomainEvent<TourBookingCancelledDomainEvent>();
        evt.PreviousStatus.Should().Be(BookingStatus.PendingConfirmation);
    }

    // ===== Complete =====

    [Fact]
    public void Complete_from_Confirmed_succeeds_and_raises_event()
    {
        var booking = CreateBooking();
        booking.Confirm(ConfirmationSource.PaymentWebhook);

        booking.Complete(CompletedBy);

        booking.Status.Should().Be(BookingStatus.Completed);
        booking.CompletedByUserId.Should().Be(CompletedBy);
        var evt = booking.ShouldContainDomainEvent<TourBookingCompletedDomainEvent>();
        evt.CompletedByUserId.Should().Be(CompletedBy);
    }

    [Fact]
    public void Complete_from_non_Confirmed_state_throws()
    {
        var booking = CreateBooking();

        var act = () => booking.Complete(CompletedBy);

        act.Should().Throw<BusinessRuleViolationException>();
    }

    [Fact]
    public void Complete_with_empty_user_id_throws()
    {
        var booking = CreateBooking();
        booking.Confirm(ConfirmationSource.PaymentWebhook);

        var act = () => booking.Complete(Guid.Empty);

        act.Should().Throw<BusinessRuleViolationException>();
    }

    // ===== MoveToAwaitingPaymentExpired =====

    [Fact]
    public void MoveToAwaitingPaymentExpired_from_AwaitingPayment_cancels_with_zero_refund()
    {
        var booking = CreateBooking();

        booking.MoveToAwaitingPaymentExpired();

        booking.Status.Should().Be(BookingStatus.Cancelled);
        booking.RefundAmount.Should().Be(0m);
        booking.CancellationSource.Should().Be(CancellationSource.System);
        booking.CancellationReason.Should().Contain("Payment expired");
    }

    [Fact]
    public void MoveToAwaitingPaymentExpired_raises_PaymentExpired_domain_event()
    {
        var booking = CreateBooking();

        booking.MoveToAwaitingPaymentExpired();

        var evt = booking.ShouldContainDomainEvent<TourBookingPaymentExpiredDomainEvent>();
        evt.AvailabilitySlotId.Should().Be(SlotId);
        evt.ParticipantCount.Should().Be(2);
    }

    [Fact]
    public void MoveToAwaitingPaymentExpired_from_non_AwaitingPayment_state_throws()
    {
        var booking = CreateBooking();
        booking.Confirm(ConfirmationSource.PaymentWebhook);

        var act = () => booking.MoveToAwaitingPaymentExpired();

        act.Should().Throw<BusinessRuleViolationException>();
    }

    // ===== SetSpecialRequests =====

    [Fact]
    public void SetSpecialRequests_trims_and_stores_non_empty_value()
    {
        var booking = CreateBooking();

        booking.SetSpecialRequests("  Need vegetarian meals.  ");

        booking.SpecialRequests.Should().Be("Need vegetarian meals.");
    }

    [Fact]
    public void SetSpecialRequests_clears_to_null_for_whitespace_input()
    {
        var booking = CreateBooking();
        booking.SetSpecialRequests("something");

        booking.SetSpecialRequests("   ");

        booking.SpecialRequests.Should().BeNull();
    }
}
