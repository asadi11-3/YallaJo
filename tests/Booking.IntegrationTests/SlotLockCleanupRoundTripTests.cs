using System.Globalization;
using System.Reflection;
using Booking.Application.Interfaces;
using Booking.Domain.Entities;
using Booking.Domain.Repositories;
using Booking.Infrastructure.BackgroundServices;
using Booking.Infrastructure.BackgroundServices.Options;
using Booking.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Booking.IntegrationTests;

/// <summary>
/// EF InMemory round-trip for SlotLockCleanupService. Verifies that a SlotLock whose ExpiresAt
/// is in the past gets released through the real EF repository + UoW path. Domain-event flow
/// (SlotLockReleasedDomainEvent -&gt; outbox row) is exercised separately by unit tests on the
/// converter; this test focuses on persistence + idempotency.
/// </summary>
public sealed class SlotLockCleanupRoundTripTests
{
    [Fact]
    public async Task RunOnceAsync_releases_expired_slot_lock_via_real_repository()
    {
        var options = new DbContextOptionsBuilder<BookingDbContext>()
            .UseInMemoryDatabase($"booking-slotlock-cleanup-{Guid.NewGuid()}")
            .Options;

        await using var context = new BookingDbContext(options);

        // Seed the parent AvailabilitySlot first — the SlotLock query filter requires the
        // navigation to be present and not soft-deleted (HasQueryFilter on SlotLockConfiguration).
        var availabilitySlot = AvailabilitySlot.CreateForTour(
            tourGuideId: Guid.NewGuid(),
            tourId: Guid.NewGuid(),
            date: DateOnly.FromDateTime(DateTime.UtcNow).AddDays(3),
            start: new TimeOnly(9, 0),
            end: new TimeOnly(12, 0),
            maxCapacity: 10);
        availabilitySlot.ClearDomainEvents();
        context.AvailabilitySlots.Add(availabilitySlot);

        // Seed: an expired SlotLock pointing at the seeded slot.
        var slotLock = SlotLock.Create(
            userId: Guid.NewGuid(),
            availabilitySlotId: availabilitySlot.Id,
            participantCount: 2,
            ttl: TimeSpan.FromMinutes(5));

        // Backdate ExpiresAt so the repo query treats it as expired.
        SetProperty(slotLock, nameof(SlotLock.ExpiresAt), DateTime.UtcNow.AddMinutes(-1));
        slotLock.ClearDomainEvents();
        context.SlotLocks.Add(slotLock);
        await context.SaveChangesAsync();

        var slotRepo = CreateSlotLockRepository(context);
        var uow = new TestBookingUnitOfWork(context);
        var scopeFactory = new RoundTripServiceScopeFactory(new Dictionary<Type, object>
        {
            [typeof(ISlotLockRepository)] = slotRepo,
            [typeof(IBookingUnitOfWork)] = uow,
        });

        var serviceOptions = Options.Create(new SlotLockCleanupOptions
        {
            Enabled = true,
            Interval = TimeSpan.FromMinutes(5),
            InitialDelay = TimeSpan.Zero,
            BatchSize = 100,
        });
        IOptionsMonitor<SlotLockCleanupOptions> monitor = new TestOptionsMonitor<SlotLockCleanupOptions>(serviceOptions.Value);

        var statusStore = new BookingBackgroundServiceStatusStore();
        var service = new SlotLockCleanupService(
            scopeFactory,
            monitor,
            statusStore,
            NullLogger<SlotLockCleanupService>.Instance,
            TimeProvider.System);

        var processed = await service.RunOnceAsync(CancellationToken.None);

        processed.Should().Be(1);
        var reloaded = await context.SlotLocks.SingleAsync(l => l.Id == slotLock.Id);
        reloaded.IsReleased.Should().BeTrue();
        reloaded.ReleasedAt.Should().NotBeNull();

        // Idempotency: a second run finds no expired-active locks and saves nothing.
        var secondRun = await service.RunOnceAsync(CancellationToken.None);
        secondRun.Should().Be(0);
    }

    private static ISlotLockRepository CreateSlotLockRepository(BookingDbContext context)
    {
        var repoType = typeof(Booking.Infrastructure.DependencyInjection).Assembly
            .GetType("Booking.Infrastructure.Repositories.SlotLockRepository")
            ?? throw new InvalidOperationException("SlotLockRepository type not found.");

        var instance = Activator.CreateInstance(
            repoType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: [context],
            culture: CultureInfo.InvariantCulture);

        return (ISlotLockRepository)(instance
            ?? throw new InvalidOperationException("Failed to instantiate SlotLockRepository."));
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
