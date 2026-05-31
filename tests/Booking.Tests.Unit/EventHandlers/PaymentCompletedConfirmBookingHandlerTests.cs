using Booking.Domain.Entities;
using Booking.Domain.Enums;
using Booking.Domain.Events;
using Booking.Application.Interfaces;
using Booking.Domain.ValueObjects;
using Booking.Infrastructure.EventHandlers;
using Booking.Infrastructure.Persistence;
using Finance.Contracts.IntegrationEvents;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Event;
using YallaJo.Tests.Shared;

namespace Booking.Tests.Unit.EventHandlers;

/// <summary>
/// Phase 2: Finance PaymentCompletedIntegrationEvent → Booking lifecycle wiring.
/// </summary>
public sealed class PaymentCompletedConfirmBookingHandlerTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TourId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ProviderId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid SlotId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid GuideId = Guid.Parse("66666666-6666-6666-6666-666666666666");
    private static readonly Guid BookingId = Guid.Parse("77777777-7777-7777-7777-777777777777");
    private static readonly Guid PaymentId = Guid.Parse("88888888-8888-8888-8888-888888888888");

    private static TourBooking CreateBooking(bool instant)
    {
        var pricing = new BookingPricing(
            Subtotal: 90m, DiscountAmount: 0m, LoyaltyAmount: 0m, TotalAmount: 90m,
            CommissionRate: 0.10m, CommissionAmount: 9m, Currency: "JOD",
            LineItems: new List<BookingLineItem> { new(TierType.Adult, 2, 45m, "JOD") });

        return TourBooking.Create(
            userId: UserId, tourId: TourId, providerId: ProviderId, guideId: GuideId,
            availabilitySlotId: SlotId, participantCount: 2, pricing: pricing,
            reference: BookingReference.Compose(new DateOnly(2026, 7, 1), "A7X3K9"),
            refundPolicySnapshot: "{}", isInstantBooking: instant,
            paymentExpiresAt: DateTime.UtcNow.AddMinutes(10), lineItemsJson: "[]");
    }

    private static IntegrationEventNotification<PaymentCompletedIntegrationEvent> Notification(Guid bookingId) =>
        new(
            MessageId: Guid.NewGuid(),
            Event: new PaymentCompletedIntegrationEvent(
                PaymentId: PaymentId,
                BookingId: bookingId,
                UserId: UserId,
                ProviderId: ProviderId,
                Amount: 90m,
                Currency: "JOD",
                GatewayTransactionId: "gw-pay-1",
                CompletedAt: DateTime.UtcNow));

    private static (PaymentCompletedConfirmBookingHandler handler,
                   BookingDbContext db,
                   IBookingUnitOfWork uow) Build(TourBooking? booking)
    {
        var options = new DbContextOptionsBuilder<BookingDbContext>()
            .UseInMemoryDatabase($"booking-tests-{Guid.NewGuid():N}")
            .Options;
        var db = new BookingDbContext(options);
        if (booking is not null)
        {
            db.TourBookings.Add(booking);
            db.SaveChanges();
        }

        var uow = Substitute.For<IBookingUnitOfWork>();
        var cache = Substitute.For<HybridCache>();
        var handler = new PaymentCompletedConfirmBookingHandler(
            db, uow, cache, NullLogger<PaymentCompletedConfirmBookingHandler>.Instance);
        return (handler, db, uow);
    }

    [Fact]
    public async Task Instant_booking_payment_confirms_booking_and_raises_confirmed_event()
    {
        var booking = CreateBooking(instant: true);
        var (handler, _, uow) = Build(booking);

        await handler.Handle(Notification(booking.Id), CancellationToken.None);

        booking.Status.Should().Be(BookingStatus.Confirmed);
        booking.ConfirmationSource.Should().Be(ConfirmationSource.PaymentWebhook);
        // The confirmed domain event (consumed by the Phase 1 capacity handler) is raised.
        var evt = booking.ShouldContainDomainEvent<TourBookingConfirmedDomainEvent>();
        evt.AvailabilitySlotId.Should().Be(SlotId);
        evt.ParticipantCount.Should().Be(2);
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NonInstant_booking_payment_moves_to_pending_confirmation_without_event()
    {
        var booking = CreateBooking(instant: false);
        var (handler, _, uow) = Build(booking);

        await handler.Handle(Notification(booking.Id), CancellationToken.None);

        booking.Status.Should().Be(BookingStatus.PendingConfirmation);
        // MoveToPendingConfirmation raises no domain event (no capacity conversion yet).
        booking.DomainEvents.OfType<TourBookingConfirmedDomainEvent>().Should().BeEmpty();
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Duplicate_payment_completed_is_a_safe_noop()
    {
        var booking = CreateBooking(instant: true);
        var (handler, _, uow) = Build(booking);

        await handler.Handle(Notification(booking.Id), CancellationToken.None);   // first → Confirmed
        booking.ClearDomainEvents();
        await handler.Handle(Notification(booking.Id), CancellationToken.None);   // duplicate

        booking.Status.Should().Be(BookingStatus.Confirmed);
        booking.DomainEvents.OfType<TourBookingConfirmedDomainEvent>().Should().BeEmpty();
        // Only the first delivery saved; the duplicate short-circuits on the state guard.
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(BookingStatus.PendingConfirmation)]
    [InlineData(BookingStatus.Confirmed)]
    [InlineData(BookingStatus.Cancelled)]
    public async Task Payment_completed_for_non_awaiting_booking_is_noop(BookingStatus status)
    {
        // PendingConfirmation is only reachable for non-instant bookings.
        var booking = CreateBooking(instant: status != BookingStatus.PendingConfirmation);
        // Drive booking out of AwaitingPayment.
        switch (status)
        {
            case BookingStatus.PendingConfirmation:
                booking.MoveToPendingConfirmation();
                break;
            case BookingStatus.Confirmed:
                booking.Confirm(ConfirmationSource.Manual);
                break;
            case BookingStatus.Cancelled:
                booking.Cancel(new BookingCancellationContext(
                    CancellationSource.User, "changed mind aaaa", true, false), 0m);
                break;
        }

        booking.ClearDomainEvents();
        var (handler, _, uow) = Build(booking);

        await handler.Handle(Notification(booking.Id), CancellationToken.None);

        booking.Status.Should().Be(status);
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Booking_not_found_throws_for_retry_and_does_not_save()
    {
        var (handler, _, uow) = Build(booking: null);

        var act = async () => await handler.Handle(Notification(BookingId), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
