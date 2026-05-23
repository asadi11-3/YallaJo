using Booking.Application.Caching;
using Booking.Application.Commands.CreateBulkAvailabilitySlots;
using Booking.Application.Interfaces;
using Booking.Domain.Entities;
using Booking.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using NSubstitute;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Booking.Tests.Unit.Commands;

public sealed class CreateBulkAvailabilitySlotsHandlerTests
{
    [Fact]
    public void RecurrenceExpander_daily_generates_each_date()
    {
        var start = new DateOnly(2026, 7, 1);
        var end = new DateOnly(2026, 7, 3);

        var dates = AvailabilitySlotRecurrenceExpander.Expand(start, end, AvailabilityRecurrence.Daily, null);

        dates.Should().HaveCount(3);
        dates.Should().ContainInOrder(start, start.AddDays(1), start.AddDays(2));
    }

    [Fact]
    public void RecurrenceExpander_weekly_generates_every_7_days()
    {
        var start = new DateOnly(2026, 7, 1); // Wed
        var end = new DateOnly(2026, 7, 22);

        var dates = AvailabilitySlotRecurrenceExpander.Expand(start, end, AvailabilityRecurrence.Weekly, null);

        dates.Should().HaveCount(4);
        dates[0].Should().Be(start);
        dates[1].Should().Be(start.AddDays(7));
    }

    [Fact]
    public void RecurrenceExpander_custom_filters_days_of_week()
    {
        var start = new DateOnly(2026, 7, 1); // Wed
        var end = new DateOnly(2026, 7, 7);

        var dates = AvailabilitySlotRecurrenceExpander.Expand(
            start,
            end,
            AvailabilityRecurrence.Custom,
            [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday]);

        dates.Should().Contain(d => d.DayOfWeek == DayOfWeek.Monday);
        dates.Should().Contain(d => d.DayOfWeek == DayOfWeek.Wednesday);
        dates.Should().Contain(d => d.DayOfWeek == DayOfWeek.Friday);
    }

    [Fact]
    public async Task Skip_existing_true_skips_identical_slots()
    {
        var repo = Substitute.For<IAvailabilitySlotRepository>();
        var tourReader = Substitute.For<IBookingTourSnapshotReader>();
        var providerReader = Substitute.For<IBookingProviderSnapshotReader>();
        var uow = Substitute.For<IBookingUnitOfWork>();
        var cache = Substitute.For<HybridCache>();
        var currentUser = Substitute.For<ICurrentUser>();
        var logger = Substitute.For<ILogger<CreateBulkAvailabilitySlotsCommandHandler>>();

        var userId = Guid.NewGuid();
        var tourId = Guid.NewGuid();
        var providerId = Guid.NewGuid();

        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(userId);
        tourReader.GetByIdAsync(tourId, Arg.Any<CancellationToken>())
            .Returns(new BookingTourSnapshot(tourId, providerId, "T", "JOD", 10m, true, true, false, null, null, 30));
        providerReader.GetByIdAsync(providerId, Arg.Any<CancellationToken>())
            .Returns(new BookingProviderSnapshot(providerId, userId, "P", BookingProviderStatus.Active));
        repo.GetTourGuideIdByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Guid.NewGuid());

        var existing = AvailabilitySlot.CreateForTour(
            Guid.NewGuid(),
            tourId,
            new DateOnly(2026, 7, 1),
            new TimeOnly(9, 0),
            new TimeOnly(11, 0),
            10);

        repo.GetActiveSlotsForTourInRangeAsync(tourId, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([existing]);

        var handler = new CreateBulkAvailabilitySlotsCommandHandler(repo, tourReader, providerReader, uow, cache, currentUser, logger);
        var cmd = new CreateBulkAvailabilitySlotsCommand(
            tourId,
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 7, 2),
            AvailabilityRecurrence.Daily,
            null,
            new TimeOnly(9, 0),
            new TimeOnly(11, 0),
            10,
            SkipExisting: true);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.CreatedCount.Should().Be(1);
        result.Value.SkippedDates.Should().Contain(new DateOnly(2026, 7, 1));
    }

    [Fact]
    public async Task Skip_existing_false_returns_conflict_on_collision()
    {
        var repo = Substitute.For<IAvailabilitySlotRepository>();
        var tourReader = Substitute.For<IBookingTourSnapshotReader>();
        var providerReader = Substitute.For<IBookingProviderSnapshotReader>();
        var uow = Substitute.For<IBookingUnitOfWork>();
        var cache = Substitute.For<HybridCache>();
        var currentUser = Substitute.For<ICurrentUser>();
        var logger = Substitute.For<ILogger<CreateBulkAvailabilitySlotsCommandHandler>>();

        var userId = Guid.NewGuid();
        var tourId = Guid.NewGuid();
        var providerId = Guid.NewGuid();

        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(userId);
        tourReader.GetByIdAsync(tourId, Arg.Any<CancellationToken>())
            .Returns(new BookingTourSnapshot(tourId, providerId, "T", "JOD", 10m, true, true, false, null, null, 30));
        providerReader.GetByIdAsync(providerId, Arg.Any<CancellationToken>())
            .Returns(new BookingProviderSnapshot(providerId, userId, "P", BookingProviderStatus.Active));
        repo.GetTourGuideIdByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Guid.NewGuid());

        var existing = AvailabilitySlot.CreateForTour(
            Guid.NewGuid(),
            tourId,
            new DateOnly(2026, 7, 1),
            new TimeOnly(9, 0),
            new TimeOnly(11, 0),
            10);

        repo.GetActiveSlotsForTourInRangeAsync(tourId, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([existing]);

        var handler = new CreateBulkAvailabilitySlotsCommandHandler(repo, tourReader, providerReader, uow, cache, currentUser, logger);
        var cmd = new CreateBulkAvailabilitySlotsCommand(
            tourId,
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 7, 2),
            AvailabilityRecurrence.Daily,
            null,
            new TimeOnly(9, 0),
            new TimeOnly(11, 0),
            10,
            SkipExisting: false);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors.Should().ContainSingle(e => e.Code == "AvailabilitySlot.Overlap");
    }

    [Fact]
    public async Task Caller_not_registered_as_tour_guide_returns_forbidden()
    {
        // BOOKING-P0-FIX-001 #3: parity with the single-create handler — no synthetic FK fallback.
        var repo = Substitute.For<IAvailabilitySlotRepository>();
        var tourReader = Substitute.For<IBookingTourSnapshotReader>();
        var providerReader = Substitute.For<IBookingProviderSnapshotReader>();
        var uow = Substitute.For<IBookingUnitOfWork>();
        var cache = Substitute.For<HybridCache>();
        var currentUser = Substitute.For<ICurrentUser>();
        var logger = Substitute.For<ILogger<CreateBulkAvailabilitySlotsCommandHandler>>();

        var userId = Guid.NewGuid();
        var tourId = Guid.NewGuid();
        var providerId = Guid.NewGuid();

        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(userId);
        tourReader.GetByIdAsync(tourId, Arg.Any<CancellationToken>())
            .Returns(new BookingTourSnapshot(tourId, providerId, "T", "JOD", 10m, true, true, false, null, null, 30));
        providerReader.GetByIdAsync(providerId, Arg.Any<CancellationToken>())
            .Returns(new BookingProviderSnapshot(providerId, userId, "P", BookingProviderStatus.Active));
        // Critical: tour-guide lookup returns null → handler must short-circuit with Forbidden
        // BEFORE touching slots or saving.
        repo.GetTourGuideIdByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((Guid?)null);

        var handler = new CreateBulkAvailabilitySlotsCommandHandler(repo, tourReader, providerReader, uow, cache, currentUser, logger);
        var cmd = new CreateBulkAvailabilitySlotsCommand(
            tourId,
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 7, 2),
            AvailabilityRecurrence.Daily,
            null,
            new TimeOnly(9, 0),
            new TimeOnly(11, 0),
            10,
            SkipExisting: true);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Forbidden);
        result.Errors.Should().ContainSingle(e => e.Code == "AvailabilitySlot.NotAProvider");
        await repo.DidNotReceive().AddRangeAsync(
            Arg.Any<IEnumerable<AvailabilitySlot>>(), Arg.Any<CancellationToken>());
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── BOOKING-P1-CACHE-STANDARD-FIX-001 §4 — cache invalidation ────────────

    [Fact]
    public async Task Successful_bulk_create_invalidates_TourTag_and_every_distinct_TourDateTag()
    {
        var repo = Substitute.For<IAvailabilitySlotRepository>();
        var tourReader = Substitute.For<IBookingTourSnapshotReader>();
        var providerReader = Substitute.For<IBookingProviderSnapshotReader>();
        var uow = Substitute.For<IBookingUnitOfWork>();
        var cache = Substitute.For<HybridCache>();
        var currentUser = Substitute.For<ICurrentUser>();
        var logger = Substitute.For<ILogger<CreateBulkAvailabilitySlotsCommandHandler>>();

        var userId = Guid.NewGuid();
        var tourId = Guid.NewGuid();
        var providerId = Guid.NewGuid();

        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(userId);
        tourReader.GetByIdAsync(tourId, Arg.Any<CancellationToken>())
            .Returns(new BookingTourSnapshot(tourId, providerId, "T", "JOD", 10m, true, true, false, null, null, 30));
        providerReader.GetByIdAsync(providerId, Arg.Any<CancellationToken>())
            .Returns(new BookingProviderSnapshot(providerId, userId, "P", BookingProviderStatus.Active));
        repo.GetTourGuideIdByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Guid.NewGuid());

        // 3-day daily window — handler will produce 3 slots on 3 distinct dates.
        var start = new DateOnly(2026, 7, 1);
        var end = new DateOnly(2026, 7, 3);

        var handler = new CreateBulkAvailabilitySlotsCommandHandler(repo, tourReader, providerReader, uow, cache, currentUser, logger);
        var cmd = new CreateBulkAvailabilitySlotsCommand(
            tourId,
            start,
            end,
            AvailabilityRecurrence.Daily,
            null,
            new TimeOnly(9, 0),
            new TimeOnly(11, 0),
            10,
            SkipExisting: true);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.CreatedCount.Should().Be(3);

        // TourTag — one call.
        await cache.Received(1).RemoveByTagAsync(
            BookingAvailabilityCacheKeys.TourTag(tourId), Arg.Any<CancellationToken>());

        // Each affected date — exactly one call per distinct date.
        for (var d = start; d <= end; d = d.AddDays(1))
        {
            await cache.Received(1).RemoveByTagAsync(
                BookingAvailabilityCacheKeys.TourDateTag(tourId, d), Arg.Any<CancellationToken>());
        }
    }

    [Fact]
    public async Task Bulk_create_does_not_invalidate_cache_on_failure()
    {
        // Non-provider Forbidden path — handler short-circuits before SaveChanges, so cache
        // MUST stay untouched (per the post-SaveChanges-only invalidation rule).
        var repo = Substitute.For<IAvailabilitySlotRepository>();
        var tourReader = Substitute.For<IBookingTourSnapshotReader>();
        var providerReader = Substitute.For<IBookingProviderSnapshotReader>();
        var uow = Substitute.For<IBookingUnitOfWork>();
        var cache = Substitute.For<HybridCache>();
        var currentUser = Substitute.For<ICurrentUser>();
        var logger = Substitute.For<ILogger<CreateBulkAvailabilitySlotsCommandHandler>>();

        var userId = Guid.NewGuid();
        var tourId = Guid.NewGuid();
        var providerId = Guid.NewGuid();

        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(userId);
        tourReader.GetByIdAsync(tourId, Arg.Any<CancellationToken>())
            .Returns(new BookingTourSnapshot(tourId, providerId, "T", "JOD", 10m, true, true, false, null, null, 30));
        providerReader.GetByIdAsync(providerId, Arg.Any<CancellationToken>())
            .Returns(new BookingProviderSnapshot(providerId, userId, "P", BookingProviderStatus.Active));
        repo.GetTourGuideIdByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((Guid?)null);

        var handler = new CreateBulkAvailabilitySlotsCommandHandler(repo, tourReader, providerReader, uow, cache, currentUser, logger);
        var cmd = new CreateBulkAvailabilitySlotsCommand(
            tourId,
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 7, 2),
            AvailabilityRecurrence.Daily,
            null,
            new TimeOnly(9, 0),
            new TimeOnly(11, 0),
            10,
            SkipExisting: true);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
