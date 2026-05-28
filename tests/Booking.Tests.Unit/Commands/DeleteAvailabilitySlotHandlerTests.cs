using Booking.Application.Commands.DeleteAvailabilitySlot;
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

public sealed class DeleteAvailabilitySlotHandlerTests
{
    private static AvailabilitySlot CreateSlot(Guid tourId, int maxCapacity = 10)
        => AvailabilitySlot.CreateForTour(Guid.NewGuid(), tourId, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(2), new TimeOnly(9, 0), new TimeOnly(11, 0), maxCapacity);

    [Fact]
    public async Task Delete_without_bookings_succeeds()
    {
        var repo = Substitute.For<IAvailabilitySlotRepository>();
        var tourReader = Substitute.For<IBookingTourSnapshotReader>();
        var providerReader = Substitute.For<IBookingProviderSnapshotReader>();
        var uow = Substitute.For<IBookingUnitOfWork>();
        var cache = Substitute.For<HybridCache>();
        var currentUser = Substitute.For<ICurrentUser>();
        var logger = Substitute.For<ILogger<DeleteAvailabilitySlotCommandHandler>>();

        var userId = Guid.NewGuid();
        var tourId = Guid.NewGuid();
        var providerId = Guid.NewGuid();
        var slot = CreateSlot(tourId);

        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(userId);
        repo.GetByIdWithLockAsync(slot.Id, Arg.Any<CancellationToken>()).Returns(slot);
        tourReader.GetByIdAsync(tourId, Arg.Any<CancellationToken>())
            .Returns(new BookingTourSnapshot(tourId, providerId, "T", "JOD", 10m, true, true, false, null, null, 20));
        providerReader.GetByIdAsync(providerId, Arg.Any<CancellationToken>())
            .Returns(new BookingProviderSnapshot(providerId, userId, "P", BookingProviderStatus.Active));

        var handler = new DeleteAvailabilitySlotCommandHandler(repo, tourReader, providerReader, uow, cache, currentUser, logger);

        var result = await handler.Handle(new DeleteAvailabilitySlotCommand(slot.Id, []), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_with_bookings_returns_conflict()
    {
        var repo = Substitute.For<IAvailabilitySlotRepository>();
        var tourReader = Substitute.For<IBookingTourSnapshotReader>();
        var providerReader = Substitute.For<IBookingProviderSnapshotReader>();
        var uow = Substitute.For<IBookingUnitOfWork>();
        var cache = Substitute.For<HybridCache>();
        var currentUser = Substitute.For<ICurrentUser>();
        var logger = Substitute.For<ILogger<DeleteAvailabilitySlotCommandHandler>>();

        var userId = Guid.NewGuid();
        var tourId = Guid.NewGuid();
        var providerId = Guid.NewGuid();
        var slot = CreateSlot(tourId);
        slot.Book(1);

        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(userId);
        repo.GetByIdWithLockAsync(slot.Id, Arg.Any<CancellationToken>()).Returns(slot);
        tourReader.GetByIdAsync(tourId, Arg.Any<CancellationToken>())
            .Returns(new BookingTourSnapshot(tourId, providerId, "T", "JOD", 10m, true, true, false, null, null, 20));
        providerReader.GetByIdAsync(providerId, Arg.Any<CancellationToken>())
            .Returns(new BookingProviderSnapshot(providerId, userId, "P", BookingProviderStatus.Active));

        var handler = new DeleteAvailabilitySlotCommandHandler(repo, tourReader, providerReader, uow, cache, currentUser, logger);

        var result = await handler.Handle(new DeleteAvailabilitySlotCommand(slot.Id, []), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors.Should().ContainSingle(e => e.Code == "AvailabilitySlot.HasBookings");
    }
}
