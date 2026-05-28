using System.Globalization;
using System.Reflection;
using Booking.Application.Interfaces;
using Booking.Domain.Entities;
using Booking.Domain.Enums;
using Booking.Domain.Events;
using Booking.Domain.Repositories;
using Booking.Domain.ValueObjects;
using Booking.Infrastructure.BackgroundServices;
using Booking.Infrastructure.BackgroundServices.Options;
using Booking.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Booking.IntegrationTests;

public sealed class BookingAutoExpireRoundTripTests
{
    [Fact]
    public async Task RunOnceAsync_cancels_awaiting_payment_booking_past_payment_expires_at()
    {
        var options = new DbContextOptionsBuilder<BookingDbContext>()
            .UseInMemoryDatabase($"booking-auto-expire-{Guid.NewGuid()}")
            .Options;

        await using var context = new BookingDbContext(options);

        // Build a TourBooking with PaymentExpiresAt in the past. The factory requires
        // PaymentExpiresAt to be in the FUTURE; we backdate it via reflection after construction.
        var booking = CreateAwaitingPaymentBooking();
        SetProperty(booking, nameof(TourBooking.PaymentExpiresAt), DateTime.UtcNow.AddMinutes(-1));
        booking.ClearDomainEvents();
        context.TourBookings.Add(booking);
        await context.SaveChangesAsync();

        var repo = CreateTourBookingRepository(context);
        var uow = new TestBookingUnitOfWork(context);
        var scopeFactory = new RoundTripServiceScopeFactory(new Dictionary<Type, object>
        {
            [typeof(ITourBookingRepository)] = repo,
            [typeof(IBookingUnitOfWork)] = uow,
        });

        var monitor = new TestOptionsMonitor<BookingAutoExpireOptions>(new BookingAutoExpireOptions
        {
            Enabled = true,
            Interval = TimeSpan.FromMinutes(5),
            InitialDelay = TimeSpan.Zero,
            BatchSize = 100,
            PaymentWindowMinutes = 10,
        });

        var statusStore = new BookingBackgroundServiceStatusStore();
        var service = new BookingAutoExpireService(
            scopeFactory,
            monitor,
            statusStore,
            NullLogger<BookingAutoExpireService>.Instance,
            TimeProvider.System);

        var processed = await service.RunOnceAsync(CancellationToken.None);

        processed.Should().Be(1);
        var reloaded = await context.TourBookings.SingleAsync(b => b.Id == booking.Id);
        reloaded.Status.Should().Be(BookingStatus.Cancelled);
        reloaded.CancellationSource.Should().Be(CancellationSource.System);
        reloaded.RefundAmount.Should().Be(0m);

        // Domain events fire on the in-memory aggregate before SaveChanges; the reloaded
        // entity (different tracker state) won't have them, but the originally-tracked
        // booking still has the payment-expired event for inspection.
        booking.DomainEvents
            .OfType<TourBookingPaymentExpiredDomainEvent>()
            .Should().ContainSingle();

        // Idempotency: second pass finds nothing (booking now Cancelled).
        var secondRun = await service.RunOnceAsync(CancellationToken.None);
        secondRun.Should().Be(0);
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
        return TourBooking.Create(
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
    }

    private static ITourBookingRepository CreateTourBookingRepository(BookingDbContext context)
    {
        var repoType = typeof(Booking.Infrastructure.DependencyInjection).Assembly
            .GetType("Booking.Infrastructure.Repositories.TourBookingRepository")
            ?? throw new InvalidOperationException("TourBookingRepository type not found.");

        var instance = Activator.CreateInstance(
            repoType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: [context],
            culture: CultureInfo.InvariantCulture);

        return (ITourBookingRepository)(instance
            ?? throw new InvalidOperationException("Failed to instantiate TourBookingRepository."));
    }

    private static void SetProperty<TValue>(object target, string propertyName, TValue value)
    {
        var property = target.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Property '{propertyName}' not found.");

        property.SetValue(target, value);
    }

    private sealed class TestBookingUnitOfWork(BookingDbContext context) : IBookingUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => context.SaveChangesAsync(cancellationToken);
    }

    private sealed class TestOptionsMonitor<T>(T value) : IOptionsMonitor<T> where T : class
    {
        public T CurrentValue { get; } = value;
        public T Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }

    private sealed class RoundTripServiceScopeFactory(IReadOnlyDictionary<Type, object> services)
        : IServiceScopeFactory, IServiceScope, IServiceProvider
    {
        private readonly Dictionary<Type, object> _services = new(services);

        public IServiceProvider ServiceProvider => this;
        public IServiceScope CreateScope() => this;

        public object? GetService(Type serviceType)
            => _services.TryGetValue(serviceType, out var instance) ? instance : null;

        public void Dispose() { }
    }
}
