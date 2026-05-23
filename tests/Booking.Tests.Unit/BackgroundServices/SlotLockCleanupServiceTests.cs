using System.Reflection;
using Booking.Application.Interfaces;
using Booking.Domain.Entities;
using Booking.Domain.Events;
using Booking.Domain.Repositories;
using Booking.Infrastructure.BackgroundServices;
using Booking.Infrastructure.BackgroundServices.Options;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using YallaJo.Tests.Shared;

namespace Booking.Tests.Unit.BackgroundServices;

public sealed class SlotLockCleanupServiceTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid SlotId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task RunOnceAsync_releases_expired_active_locks_and_saves_once()
    {
        var (service, slotRepo, uow, _) = BuildSubject(out var _);
        var expired = CreateExpiredLock();

        slotRepo
            .GetExpiredActiveAsync(Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<SlotLock> { expired });

        var processed = await service.RunOnceAsync(CancellationToken.None);

        processed.Should().Be(1);
        expired.IsReleased.Should().BeTrue();
        expired.DomainEvents.OfType<SlotLockReleasedDomainEvent>().Should().ContainSingle();
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunOnceAsync_with_no_expired_locks_does_not_save()
    {
        var (service, slotRepo, uow, _) = BuildSubject(out var _);

        slotRepo
            .GetExpiredActiveAsync(Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<SlotLock>());

        var processed = await service.RunOnceAsync(CancellationToken.None);

        processed.Should().Be(0);
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunOnceAsync_release_is_idempotent_on_already_released_lock()
    {
        var (service, slotRepo, uow, _) = BuildSubject(out var _);

        var alreadyReleased = CreateExpiredLock();
        alreadyReleased.Release(); // first release populates IsReleased=true
        alreadyReleased.ClearDomainEvents();

        slotRepo
            .GetExpiredActiveAsync(Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<SlotLock> { alreadyReleased });

        var processed = await service.RunOnceAsync(CancellationToken.None);

        // Service still iterates the row (repo returned it), but Release() is a no-op
        // and no new SlotLockReleasedDomainEvent is raised.
        processed.Should().Be(1);
        alreadyReleased.DomainEvents.OfType<SlotLockReleasedDomainEvent>().Should().BeEmpty();
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunOnceAsync_propagates_repository_exception()
    {
        var (service, slotRepo, uow, _) = BuildSubject(out var _);

        slotRepo
            .GetExpiredActiveAsync(Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<SlotLock>>(_ => throw new InvalidOperationException("DB blew up"));

        await FluentActions
            .Invoking(() => service.RunOnceAsync(CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>();

        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static (
        SlotLockCleanupService Service,
        ISlotLockRepository SlotRepo,
        IBookingUnitOfWork Uow,
        IBookingBackgroundServiceStatusStore StatusStore) BuildSubject(out FakeTimeProvider timeProvider)
    {
        var slotRepo = Substitute.For<ISlotLockRepository>();
        var uow = Substitute.For<IBookingUnitOfWork>();
        IBookingBackgroundServiceStatusStore statusStore = new BookingBackgroundServiceStatusStore();
        timeProvider = new FakeTimeProvider(DateTimeOffset.Parse(
            "2026-07-15T12:00:00Z",
            System.Globalization.CultureInfo.InvariantCulture));

        var scopeFactory = new TestServiceScopeFactory(new Dictionary<Type, object>
        {
            [typeof(ISlotLockRepository)] = slotRepo,
            [typeof(IBookingUnitOfWork)] = uow,
        });

        var options = new InlineOptionsMonitor<SlotLockCleanupOptions>(new SlotLockCleanupOptions
        {
            Enabled = true,
            Interval = TimeSpan.FromMinutes(5),
            InitialDelay = TimeSpan.Zero,
            BatchSize = 100,
        });

        var service = new SlotLockCleanupService(
            scopeFactory,
            options,
            statusStore,
            NullLogger<SlotLockCleanupService>.Instance,
            timeProvider);

        return (service, slotRepo, uow, statusStore);
    }

    /// <summary>
    /// Builds a SlotLock whose <c>ExpiresAt</c> is in the past. The factory always stamps
    /// <c>now + ttl</c>, so we reflect on the property to backdate the column.
    /// </summary>
    private static SlotLock CreateExpiredLock()
    {
        var slotLock = SlotLock.Create(UserId, SlotId, participantCount: 2, ttl: TimeSpan.FromMinutes(5));
        SetProperty(slotLock, nameof(SlotLock.ExpiresAt), DateTime.UtcNow.AddMinutes(-1));
        slotLock.ClearDomainEvents();
        return slotLock;
    }

    private static void SetProperty<TValue>(object target, string propertyName, TValue value)
    {
        var property = target.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Property '{propertyName}' not found on {target.GetType().Name}.");

        property.SetValue(target, value);
    }
}
