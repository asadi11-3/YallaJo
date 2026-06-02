using Accounts.Contracts.IntegrationEvents;
using Booking.Application.Interfaces;
using Booking.Domain.Entities;
using Booking.Domain.Enums;
using Booking.Domain.ValueObjects;
using Booking.Infrastructure.EventHandlers;
using Booking.Infrastructure.Persistence;
using ContentTours.Contracts;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Tests.Unit.EventHandlers;

public sealed class SuspensionCancelBookingsHandlerTests
{
    private static readonly Guid ProviderUserId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid GuideUserId = Guid.Parse("66666666-6666-6666-6666-666666666666");
    private static readonly Guid TourId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid SlotId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private static TourBooking CreateBooking(Guid providerId, Guid guideId)
    {
        var pricing = new BookingPricing(
            Subtotal: 90m, DiscountAmount: 0m, LoyaltyAmount: 0m, TotalAmount: 90m,
            CommissionRate: 0.10m, CommissionAmount: 9m, Currency: "JOD",
            LineItems: new List<BookingLineItem> { new(TierType.Adult, 2, 45m, "JOD") });

        return TourBooking.Create(
            userId: Guid.NewGuid(), tourId: TourId, providerId: providerId, guideId: guideId,
            availabilitySlotId: SlotId, participantCount: 2, pricing: pricing,
            reference: BookingReference.Compose(new DateOnly(2026, 7, 1), NextSuffix()),
            refundPolicySnapshot: "{}", isInstantBooking: true,
            paymentExpiresAt: DateTime.UtcNow.AddMinutes(10), lineItemsJson: "[]");
    }

    // BookingReference alphabet (excludes O, 0, I, 1). Produces unique 6-char suffixes so the
    // unique Reference index is never violated across bookings within a test.
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private static int _suffixCounter;
    private static string NextSuffix()
    {
        var n = Interlocked.Increment(ref _suffixCounter);
        Span<char> buf = stackalloc char[6];
        for (var i = 5; i >= 0; i--)
        {
            buf[i] = Alphabet[n & 31];
            n >>= 5;
        }
        return new string(buf);
    }

    private static BookingDbContext NewDb() =>
        new(new DbContextOptionsBuilder<BookingDbContext>()
            .UseInMemoryDatabase($"booking-suspend-tests-{Guid.NewGuid():N}")
            .Options);

    private static IntegrationEventNotification<ProviderSuspendedIntegrationEvent> ProviderNotification() =>
        new(
            MessageId: Guid.NewGuid(),
            Event: new ProviderSuspendedIntegrationEvent(
                ApplicationId: Guid.NewGuid(),
                UserId: ProviderUserId,
                Reason: "Policy violation under review",
                SuspendedAt: DateTime.UtcNow));

    private static IntegrationEventNotification<TourGuideSuspendedIntegrationEvent> GuideNotification() =>
        new(
            MessageId: Guid.NewGuid(),
            Event: new TourGuideSuspendedIntegrationEvent(
                TourGuideId: Guid.NewGuid(),
                UserId: GuideUserId,
                Reason: "Policy violation under review",
                SuspendedByAdminId: Guid.NewGuid(),
                SuspendedAt: DateTime.UtcNow));

    // ── Provider handler ─────────────────────────────────────────────────────

    [Fact]
    public async Task Provider_handler_cancels_active_bookings()
    {
        using var db = NewDb();
        var a = CreateBooking(ProviderUserId, GuideUserId);
        var b = CreateBooking(ProviderUserId, GuideUserId);
        db.TourBookings.AddRange(a, b);
        await db.SaveChangesAsync();

        var uow = Substitute.For<IBookingUnitOfWork>();
        var handler = new ProviderSuspendedCancelBookingsHandler(
            db, uow, NullLogger<ProviderSuspendedCancelBookingsHandler>.Instance);

        await handler.Handle(ProviderNotification(), CancellationToken.None);

        a.Status.Should().Be(BookingStatus.Cancelled);
        b.Status.Should().Be(BookingStatus.Cancelled);
        a.RefundAmount.Should().Be(90m); // force-majeure => 100%
        await uow.Received().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Provider_handler_skips_booking_that_became_terminal_and_continues_batch()
    {
        using var db = NewDb();
        // Two active bookings persisted (both match the active-status query filter).
        var active = CreateBooking(ProviderUserId, GuideUserId);
        var racer = CreateBooking(ProviderUserId, GuideUserId);
        db.TourBookings.AddRange(active, racer);
        await db.SaveChangesAsync();

        // Simulate the race: the tracked 'racer' instance the handler will load is already
        // terminal (e.g. the user cancelled it, or a duplicate delivery handled it first),
        // yet it would still be returned by an in-flight active-status query snapshot.
        racer.Cancel(new BookingCancellationContext(
            CancellationSource.User, Reason: null, ProviderInitiated: false, ForceMajeureOverride: false), 0m);

        var uow = Substitute.For<IBookingUnitOfWork>();
        var handler = new ProviderSuspendedCancelBookingsHandler(
            db, uow, NullLogger<ProviderSuspendedCancelBookingsHandler>.Instance);

        // Must NOT throw — the BusinessRuleViolationException for the terminal booking is caught.
        var act = async () => await handler.Handle(ProviderNotification(), CancellationToken.None);
        await act.Should().NotThrowAsync();

        // The healthy booking was still cancelled (batch continued).
        active.Status.Should().Be(BookingStatus.Cancelled);
        // The racer stays in its original terminal state (skipped, not re-cancelled).
        racer.CancellationSource.Should().Be(CancellationSource.User);
        await uow.Received().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Provider_handler_duplicate_delivery_is_safe_noop_second_time()
    {
        using var db = NewDb();
        var booking = CreateBooking(ProviderUserId, GuideUserId);
        db.TourBookings.Add(booking);
        await db.SaveChangesAsync();

        var uow = Substitute.For<IBookingUnitOfWork>();
        var handler = new ProviderSuspendedCancelBookingsHandler(
            db, uow, NullLogger<ProviderSuspendedCancelBookingsHandler>.Instance);

        // First delivery cancels the booking.
        await handler.Handle(ProviderNotification(), CancellationToken.None);
        booking.Status.Should().Be(BookingStatus.Cancelled);

        // Duplicate delivery must not throw (no poisoned retry); booking already excluded by filter.
        var act = async () => await handler.Handle(ProviderNotification(), CancellationToken.None);
        await act.Should().NotThrowAsync();
        booking.Status.Should().Be(BookingStatus.Cancelled);
    }

    // ── Guide handler (mirrors provider handler) ──────────────────────────────

    [Fact]
    public async Task Guide_handler_cancels_active_bookings()
    {
        using var db = NewDb();
        var a = CreateBooking(ProviderUserId, GuideUserId);
        db.TourBookings.Add(a);
        await db.SaveChangesAsync();

        var uow = Substitute.For<IBookingUnitOfWork>();
        var handler = new GuideSuspendedCancelBookingsHandler(
            db, uow, NullLogger<GuideSuspendedCancelBookingsHandler>.Instance);

        await handler.Handle(GuideNotification(), CancellationToken.None);

        a.Status.Should().Be(BookingStatus.Cancelled);
        a.RefundAmount.Should().Be(90m);
        await uow.Received().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Guide_handler_skips_booking_that_became_terminal_and_continues_batch()
    {
        using var db = NewDb();
        var active = CreateBooking(ProviderUserId, GuideUserId);
        var racer = CreateBooking(ProviderUserId, GuideUserId);
        db.TourBookings.AddRange(active, racer);
        await db.SaveChangesAsync();

        racer.Cancel(new BookingCancellationContext(
            CancellationSource.User, Reason: null, ProviderInitiated: false, ForceMajeureOverride: false), 0m);

        var uow = Substitute.For<IBookingUnitOfWork>();
        var handler = new GuideSuspendedCancelBookingsHandler(
            db, uow, NullLogger<GuideSuspendedCancelBookingsHandler>.Instance);

        var act = async () => await handler.Handle(GuideNotification(), CancellationToken.None);
        await act.Should().NotThrowAsync();

        active.Status.Should().Be(BookingStatus.Cancelled);
        racer.CancellationSource.Should().Be(CancellationSource.User);
        await uow.Received().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
