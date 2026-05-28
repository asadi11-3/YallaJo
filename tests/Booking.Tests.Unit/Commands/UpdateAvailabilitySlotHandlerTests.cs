using Booking.Application.Commands.UpdateAvailabilitySlot;
using Booking.Application.Interfaces;
using Booking.Domain.Entities;
using Booking.Domain.Enums;
using Booking.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using NSubstitute;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Booking.Tests.Unit.Commands;

public sealed class UpdateAvailabilitySlotHandlerTests
{
    private static AvailabilitySlot CreateSlot(Guid tourId, int maxCapacity = 10)
        => AvailabilitySlot.CreateForTour(Guid.NewGuid(), tourId, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(2), new TimeOnly(9, 0), new TimeOnly(11, 0), maxCapacity);

    [Fact]
    public async Task Success_updates_capacity()
    {
        var repo = Substitute.For<IAvailabilitySlotRepository>();
        var tourReader = Substitute.For<IBookingTourSnapshotReader>();
        var providerReader = Substitute.For<IBookingProviderSnapshotReader>();
        var uow = Substitute.For<IBookingUnitOfWork>();
        var cache = Substitute.For<HybridCache>();
        var currentUser = Substitute.For<ICurrentUser>();
        var logger = Substitute.For<ILogger<UpdateAvailabilitySlotCommandHandler>>();

        var userId = Guid.NewGuid();
        var tourId = Guid.NewGuid();
        var providerId = Guid.NewGuid();
        var slot = CreateSlot(tourId, 10);

        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(userId);
        repo.GetByIdWithLockAsync(slot.Id, Arg.Any<CancellationToken>()).Returns(slot);
        tourReader.GetByIdAsync(tourId, Arg.Any<CancellationToken>())
            .Returns(new BookingTourSnapshot(tourId, providerId, "T", "JOD", 10m, true, true, false, null, null, 20));
        providerReader.GetByIdAsync(providerId, Arg.Any<CancellationToken>())
            .Returns(new BookingProviderSnapshot(providerId, userId, "P", BookingProviderStatus.Active));

        var handler = new UpdateAvailabilitySlotCommandHandler(repo, tourReader, providerReader, uow, cache, currentUser, logger);

        var result = await handler.Handle(new UpdateAvailabilitySlotCommand(slot.Id, 14, []), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.MaxCapacity.Should().Be(14);
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Capacity_below_held_returns_conflict()
    {
        var repo = Substitute.For<IAvailabilitySlotRepository>();
        var tourReader = Substitute.For<IBookingTourSnapshotReader>();
        var providerReader = Substitute.For<IBookingProviderSnapshotReader>();
        var uow = Substitute.For<IBookingUnitOfWork>();
        var cache = Substitute.For<HybridCache>();
        var currentUser = Substitute.For<ICurrentUser>();
        var logger = Substitute.For<ILogger<UpdateAvailabilitySlotCommandHandler>>();

        var userId = Guid.NewGuid();
        var tourId = Guid.NewGuid();
        var providerId = Guid.NewGuid();
        var slot = CreateSlot(tourId, 10);
        slot.Lock(2);

        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(userId);
        repo.GetByIdWithLockAsync(slot.Id, Arg.Any<CancellationToken>()).Returns(slot);
        tourReader.GetByIdAsync(tourId, Arg.Any<CancellationToken>())
            .Returns(new BookingTourSnapshot(tourId, providerId, "T", "JOD", 10m, true, true, false, null, null, 20));
        providerReader.GetByIdAsync(providerId, Arg.Any<CancellationToken>())
            .Returns(new BookingProviderSnapshot(providerId, userId, "P", BookingProviderStatus.Active));

        var handler = new UpdateAvailabilitySlotCommandHandler(repo, tourReader, providerReader, uow, cache, currentUser, logger);

        var result = await handler.Handle(new UpdateAvailabilitySlotCommand(slot.Id, 1, []), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors.Should().ContainSingle(e => e.Code == "AvailabilitySlot.CapacityExceeded");
    }

    [Fact]
    public async Task Concurrency_conflict_returns_stale_row_version()
    {
        var repo = Substitute.For<IAvailabilitySlotRepository>();
        var tourReader = Substitute.For<IBookingTourSnapshotReader>();
        var providerReader = Substitute.For<IBookingProviderSnapshotReader>();
        var uow = Substitute.For<IBookingUnitOfWork>();
        var cache = Substitute.For<HybridCache>();
        var currentUser = Substitute.For<ICurrentUser>();
        var logger = Substitute.For<ILogger<UpdateAvailabilitySlotCommandHandler>>();

        var userId = Guid.NewGuid();
        var tourId = Guid.NewGuid();
        var providerId = Guid.NewGuid();
        var slot = CreateSlot(tourId, 10);

        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(userId);
        repo.GetByIdWithLockAsync(slot.Id, Arg.Any<CancellationToken>()).Returns(slot);
        tourReader.GetByIdAsync(tourId, Arg.Any<CancellationToken>())
            .Returns(new BookingTourSnapshot(tourId, providerId, "T", "JOD", 10m, true, true, false, null, null, 20));
        providerReader.GetByIdAsync(providerId, Arg.Any<CancellationToken>())
            .Returns(new BookingProviderSnapshot(providerId, userId, "P", BookingProviderStatus.Active));
        uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns<Task<int>>(_ => throw new Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException());

        var handler = new UpdateAvailabilitySlotCommandHandler(repo, tourReader, providerReader, uow, cache, currentUser, logger);

        var result = await handler.Handle(new UpdateAvailabilitySlotCommand(slot.Id, 14, []), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors.Should().ContainSingle(e => e.Code == "AvailabilitySlot.StaleRowVersion");
    }
}
