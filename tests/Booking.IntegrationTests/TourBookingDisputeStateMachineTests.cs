using Booking.Domain.Entities;
using Booking.Domain.Enums;
using Booking.Domain.Events;
using Booking.Domain.ValueObjects;
using FluentAssertions;
using YallaJo.SharedKernel.Domain.Exceptions;

namespace Booking.IntegrationTests;

/// <summary>
/// Phase-3 WS-6: Component-level verification of the new Booking dispute lifecycle
/// (Completed → Disputed → Resolved). Covers the highest-risk invariants of
/// <see cref="TourBooking.OpenDispute"/> and <see cref="TourBooking.ResolveDispute"/>:
/// owner-only opening, state-machine gating, and the two new domain events.
///
/// These tests exercise the aggregate directly (no DbContext) — the EF mapping +
/// integration-event publish handlers are covered separately by the EF migration +
/// the existing <see cref="TourBookingIntegrationConverters"/> pattern build-verified
/// during WS-3a.
/// </summary>
public sealed class TourBookingDisputeStateMachineTests
{
    [Fact]
    public void OpenDispute_OnCompletedBooking_ByOwner_SucceedsAndRaisesDomainEvent()
    {
        var userId = Guid.NewGuid();
        var booking = CompletedBookingFor(userId);

        booking.ClearDomainEvents();
        booking.OpenDispute(userId, "The guide cancelled at the last minute without telling us.");

        booking.Status.Should().Be(BookingStatus.Disputed);
        booking.DisputedAt.Should().NotBeNull();
        booking.DisputeOpenedByUserId.Should().Be(userId);
        booking.DisputeReason.Should().Be("The guide cancelled at the last minute without telling us.");

        booking.DomainEvents.Should()
            .ContainSingle(e => e is TourBookingDisputedDomainEvent)
            .Which.Should().BeOfType<TourBookingDisputedDomainEvent>()
            .Which.BookingId.Should().Be(booking.Id);
    }

    [Fact]
    public void OpenDispute_ByNonOwner_ThrowsBusinessRuleViolation()
    {
        var ownerId = Guid.NewGuid();
        var booking = CompletedBookingFor(ownerId);

        var act = () => booking.OpenDispute(Guid.NewGuid(), "I do not own this booking but I am opening anyway.");

        act.Should().Throw<BusinessRuleViolationException>()
            .WithMessage("Only the booking owner may open a dispute.");
        booking.Status.Should().Be(BookingStatus.Completed); // no state transition
    }

    [Fact]
    public void OpenDispute_OnNonCompletedBooking_ThrowsBusinessRuleViolation()
    {
        var userId = Guid.NewGuid();
        var booking = BookingFactory(userId);
        // Booking is still in AwaitingPayment — never completed.

        var act = () => booking.OpenDispute(userId, "Trying to dispute before the tour even ran.");

        act.Should().Throw<BusinessRuleViolationException>()
            .Which.Message.Should().Contain("Must be Completed");
        booking.Status.Should().Be(BookingStatus.AwaitingPayment);
    }

    [Fact]
    public void ResolveDispute_OnDisputedBooking_ByAdmin_TransitionsToResolved_AndRaisesEvent()
    {
        var userId = Guid.NewGuid();
        var booking = CompletedBookingFor(userId);
        booking.OpenDispute(userId, "Guide failed to show up at the meeting point.");

        var adminId = Guid.NewGuid();
        booking.ClearDomainEvents();
        booking.ResolveDispute(adminId, "Refund issued; guide warned. Dispute closed.");

        booking.Status.Should().Be(BookingStatus.Resolved);
        booking.ResolvedAt.Should().NotBeNull();
        booking.ResolvedByAdminId.Should().Be(adminId);
        booking.ResolutionNotes.Should().Be("Refund issued; guide warned. Dispute closed.");

        booking.DomainEvents.Should()
            .ContainSingle(e => e is TourBookingDisputeResolvedDomainEvent)
            .Which.Should().BeOfType<TourBookingDisputeResolvedDomainEvent>()
            .Which.ResolvedByAdminId.Should().Be(adminId);
    }

    [Fact]
    public void ResolveDispute_OnNonDisputedBooking_ThrowsBusinessRuleViolation()
    {
        var userId = Guid.NewGuid();
        var booking = CompletedBookingFor(userId);
        // Skipping OpenDispute — booking is still Completed, not Disputed.

        var act = () => booking.ResolveDispute(Guid.NewGuid(), "Trying to resolve a non-existent dispute.");

        act.Should().Throw<BusinessRuleViolationException>()
            .Which.Message.Should().Contain("Must be Disputed");
        booking.Status.Should().Be(BookingStatus.Completed);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static TourBooking BookingFactory(Guid userId)
    {
        var pricing = new BookingPricing(
            Subtotal: 100m,
            DiscountAmount: 0m,
            LoyaltyAmount: 0m,
            TotalAmount: 100m,
            CommissionRate: 0.10m,
            CommissionAmount: 10m,
            Currency: "JOD",
            LineItems: Array.Empty<BookingLineItem>());

        var reference = BookingReference.Compose(DateOnly.FromDateTime(DateTime.UtcNow), "ABCDEF");

        return TourBooking.Create(
            userId: userId,
            tourId: Guid.NewGuid(),
            providerId: Guid.NewGuid(),
            guideId: Guid.NewGuid(),
            availabilitySlotId: Guid.NewGuid(),
            participantCount: 2,
            pricing: pricing,
            reference: reference,
            refundPolicySnapshot: "{\"version\":1,\"tiers\":[]}",
            isInstantBooking: true,
            paymentExpiresAt: DateTime.UtcNow.AddMinutes(15),
            lineItemsJson: "[]");
    }

    /// <summary>
    /// Returns a TourBooking that has been driven through Create → Confirm → Complete,
    /// leaving it in <see cref="BookingStatus.Completed"/> with a fresh CompletedAt
    /// (so it sits inside the 48h dispute window).
    /// </summary>
    private static TourBooking CompletedBookingFor(Guid userId)
    {
        var booking = BookingFactory(userId);
        booking.Confirm(ConfirmationSource.PaymentWebhook);
        booking.Complete(completedByUserId: Guid.NewGuid());
        return booking;
    }
}
