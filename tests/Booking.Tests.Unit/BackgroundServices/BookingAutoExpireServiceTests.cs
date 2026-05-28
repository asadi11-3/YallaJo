using System.Reflection;
using Booking.Application.Interfaces;
using Booking.Domain.Entities;
using Booking.Domain.Enums;
using Booking.Domain.Events;
using Booking.Domain.Repositories;
using Booking.Domain.ValueObjects;
using Booking.Infrastructure.BackgroundServices;
using Booking.Infrastructure.BackgroundServices.Options;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using YallaJo.Tests.Shared;

namespace Booking.Tests.Unit.BackgroundServices;

public sealed class BookingAutoExpireServiceTests
{
    [Fact]
    public async Task RunOnceAsync_uses_now_when_querying_payment_expired_rows()
    {
        var (service, repo, uow, _) = BuildSubject(out var timeProvider);

        repo
            .GetExpiredAwaitingPaymentAsync(Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<TourBooking>());

        await service.RunOnceAsync(CancellationToken.None);

        // Source-of-truth: query must pass the current UTC moment as-is.
        // PaymentWindowMinutes is only used when STAMPING bookings, never as a query offset.
        await repo.Received(1).GetExpiredAwaitingPaymentAsync(
            timeProvider.GetUtcNow().UtcDateTime,
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunOnceAsync_expires_awaiting_payment_booking_and_raises_payment_expired_event()
    {
        var (service, repo, uow, _) = BuildSubject(out _);

        var booking = CreateAwaitingPaymentBooking();
        repo
            .GetExpiredAwaitingPaymentAsync(Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<TourBooking> { booking });

        var processed = await service.RunOnceAsync(CancellationToken.None);

        processed.Should().Be(1);
        booking.Status.Should().Be(BookingStatus.Cancelled);
        booking.CancellationSource.Should().Be(CancellationSource.System);
        booking.DomainEvents.OfType<TourBookingPaymentExpiredDomainEvent>().Should().ContainSingle();
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunOnceAsync_skips_booking_whose_state_already_changed()
    {
        var (service, repo, uow, _) = BuildSubject(out _);

        var racy = CreateAwaitingPaymentBooking();
        racy.Confirm(ConfirmationSource.PaymentWebhook); // simulates webhook landing between query and save
        racy.ClearDomainEvents();

        repo
            .GetExpiredAwaitingPaymentAsync(Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<TourBooking> { racy });

        var processed = await service.RunOnceAsync(CancellationToken.None);

        processed.Should().Be(0);
        racy.Status.Should().Be(BookingStatus.Confirmed);
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunOnceAsync_returns_zero_when_no_rows_returned()
    {
        var (service, repo, uow, _) = BuildSubject(out _);
        repo
            .GetExpiredAwaitingPaymentAsync(Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<TourBooking>());

        var processed = await service.RunOnceAsync(CancellationToken.None);

        processed.Should().Be(0);
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static (
        BookingAutoExpireService Service,
        ITourBookingRepository Repo,
        IBookingUnitOfWork Uow,
        IBookingBackgroundServiceStatusStore StatusStore) BuildSubject(out FakeTimeProvider timeProvider)
    {
        var repo = Substitute.For<ITourBookingRepository>();
        var uow = Substitute.For<IBookingUnitOfWork>();
        IBookingBackgroundServiceStatusStore statusStore = new BookingBackgroundServiceStatusStore();
        timeProvider = new FakeTimeProvider(DateTimeOffset.Parse(
            "2026-07-15T12:00:00Z",
            System.Globalization.CultureInfo.InvariantCulture));

        var scopeFactory = new TestServiceScopeFactory(new Dictionary<Type, object>
        {
            [typeof(ITourBookingRepository)] = repo,
            [typeof(IBookingUnitOfWork)] = uow,
        });

        var options = new InlineOptionsMonitor<BookingAutoExpireOptions>(new BookingAutoExpireOptions
        {
            Enabled = true,
            Interval = TimeSpan.FromMinutes(5),
            InitialDelay = TimeSpan.Zero,
            BatchSize = 100,
            PaymentWindowMinutes = 10,
        });

        var service = new BookingAutoExpireService(
            scopeFactory,
            options,
            statusStore,
            NullLogger<BookingAutoExpireService>.Instance,
            timeProvider);

        return (service, repo, uow, statusStore);
    }

    private static TourBooking CreateAwaitingPaymentBooking()
    {
        var pricing = new BookingPricing(
            Subtotal: 90m,
            DiscountAmount: 0m,
            LoyaltyAmount: 0m,
            TotalAmount: 90m,
            CommissionRate: 0.10m,
            CommissionAmount: 9m,
            Currency: "JOD",
            LineItems: new List<BookingLineItem> { new(TierType.Adult, 2, 45m, "JOD") });

        var reference = BookingReference.Compose(new DateOnly(2026, 7, 1), "A7X3K9");
        var booking = TourBooking.Create(
            userId: Guid.NewGuid(),
            tourId: Guid.NewGuid(),
            providerId: Guid.NewGuid(),
            guideId: Guid.NewGuid(),
            availabilitySlotId: Guid.NewGuid(),
            participantCount: 2,
            pricing: pricing,
            reference: reference,
            refundPolicySnapshot: "{}",
            isInstantBooking: true,
            paymentExpiresAt: DateTime.UtcNow.AddMinutes(10),
            lineItemsJson: "[]");
        booking.ClearDomainEvents();
        return booking;
    }
}
