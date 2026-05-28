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

public sealed class ProviderAutoAcceptServiceTests
{
    [Fact]
    public async Task RunOnceAsync_uses_now_minus_confirmation_window_as_cutoff()
    {
        var (service, repo, uow, _) = BuildSubject(out var timeProvider, hours: 24);
        repo
            .GetPendingConfirmationOlderThanAsync(Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<TourBooking>());

        await service.RunOnceAsync(CancellationToken.None);

        var expectedCutoff = timeProvider.GetUtcNow().UtcDateTime.AddHours(-24);
        await repo.Received(1).GetPendingConfirmationOlderThanAsync(
            expectedCutoff,
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunOnceAsync_auto_confirms_pending_booking_and_raises_event_with_auto_accept_source()
    {
        var (service, repo, uow, _) = BuildSubject(out _);

        var pending = CreatePendingConfirmationBooking();
        repo
            .GetPendingConfirmationOlderThanAsync(Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<TourBooking> { pending });

        var processed = await service.RunOnceAsync(CancellationToken.None);

        processed.Should().Be(1);
        pending.Status.Should().Be(BookingStatus.Confirmed);
        pending.ConfirmationSource.Should().Be(ConfirmationSource.AutoAccept);
        pending.DomainEvents
            .OfType<TourBookingConfirmedDomainEvent>()
            .Should().ContainSingle(e => e.Source == ConfirmationSource.AutoAccept);
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunOnceAsync_skips_booking_already_confirmed_by_other_path()
    {
        var (service, repo, uow, _) = BuildSubject(out _);

        var racy = CreatePendingConfirmationBooking();
        racy.Confirm(ConfirmationSource.Manual); // provider clicked Accept in the same 15-min window
        racy.ClearDomainEvents();

        repo
            .GetPendingConfirmationOlderThanAsync(Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<TourBooking> { racy });

        var processed = await service.RunOnceAsync(CancellationToken.None);

        processed.Should().Be(0);
        racy.ConfirmationSource.Should().Be(ConfirmationSource.Manual);
        racy.DomainEvents.OfType<TourBookingConfirmedDomainEvent>().Should().BeEmpty();
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunOnceAsync_raises_confirmed_event_exactly_once_per_booking()
    {
        var (service, repo, uow, _) = BuildSubject(out _);

        var first = CreatePendingConfirmationBooking();
        var second = CreatePendingConfirmationBooking();
        repo
            .GetPendingConfirmationOlderThanAsync(Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<TourBooking> { first, second });

        var processed = await service.RunOnceAsync(CancellationToken.None);

        processed.Should().Be(2);
        first.DomainEvents.OfType<TourBookingConfirmedDomainEvent>().Should().ContainSingle();
        second.DomainEvents.OfType<TourBookingConfirmedDomainEvent>().Should().ContainSingle();
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static (
        ProviderAutoAcceptService Service,
        ITourBookingRepository Repo,
        IBookingUnitOfWork Uow,
        IBookingBackgroundServiceStatusStore StatusStore) BuildSubject(out FakeTimeProvider timeProvider, double hours = 24d)
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

        var options = new InlineOptionsMonitor<ProviderAutoAcceptOptions>(new ProviderAutoAcceptOptions
        {
            Enabled = true,
            Interval = TimeSpan.FromMinutes(15),
            InitialDelay = TimeSpan.Zero,
            BatchSize = 100,
            ProviderConfirmationHours = hours,
        });

        var service = new ProviderAutoAcceptService(
            scopeFactory,
            options,
            statusStore,
            NullLogger<ProviderAutoAcceptService>.Instance,
            timeProvider);

        return (service, repo, uow, statusStore);
    }

    private static TourBooking CreatePendingConfirmationBooking()
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
            isInstantBooking: false, // non-instant -> moves to PendingConfirmation on payment
            paymentExpiresAt: DateTime.UtcNow.AddMinutes(10),
            lineItemsJson: "[]");
        booking.MoveToPendingConfirmation();
        booking.ClearDomainEvents();
        return booking;
    }
}
