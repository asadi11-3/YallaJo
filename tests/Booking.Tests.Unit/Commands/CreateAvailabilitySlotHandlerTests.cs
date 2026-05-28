using Booking.Application.Commands.CreateAvailabilitySlot;
using Booking.Application.Interfaces;
using Booking.Domain.Enums;
using Booking.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using NSubstitute;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Booking.Tests.Unit.Commands;

public sealed class CreateAvailabilitySlotHandlerTests
{
    [Fact]
    public async Task Success_returns_created_and_calls_uow()
    {
        var repo = Substitute.For<IAvailabilitySlotRepository>();
        var tourReader = Substitute.For<IBookingTourSnapshotReader>();
        var providerReader = Substitute.For<IBookingProviderSnapshotReader>();
        var uow = Substitute.For<IBookingUnitOfWork>();
        var cache = Substitute.For<HybridCache>();
        var currentUser = Substitute.For<ICurrentUser>();
        var logger = Substitute.For<ILogger<CreateAvailabilitySlotCommandHandler>>();

        var userId = Guid.NewGuid();
        var tourId = Guid.NewGuid();
        var providerId = Guid.NewGuid();

        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(userId);

        tourReader.GetByIdAsync(tourId, Arg.Any<CancellationToken>())
            .Returns(new BookingTourSnapshot(tourId, providerId, "T", "JOD", 10m, true, true, false, null, null, 12));
        providerReader.GetByIdAsync(providerId, Arg.Any<CancellationToken>())
            .Returns(new BookingProviderSnapshot(providerId, userId, "P", BookingProviderStatus.Active));
        repo.GetTourGuideIdByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Guid.NewGuid());
        repo.AnyOverlapAsync(tourId, Arg.Any<DateOnly>(), Arg.Any<TimeOnly>(), Arg.Any<TimeOnly>(), null, Arg.Any<CancellationToken>())
            .Returns(false);

        var handler = new CreateAvailabilitySlotCommandHandler(repo, tourReader, providerReader, uow, cache, currentUser, logger);
        var cmd = new CreateAvailabilitySlotCommand(
            TourId: tourId,
            Date: DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1),
            StartTime: new TimeOnly(9, 0),
            EndTime: new TimeOnly(11, 0),
            MaxCapacity: 8);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Created);
        await repo.Received(1).AddAsync(Arg.Any<Booking.Domain.Entities.AvailabilitySlot>(), Arg.Any<CancellationToken>());
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Overlap_returns_conflict()
    {
        var repo = Substitute.For<IAvailabilitySlotRepository>();
        var tourReader = Substitute.For<IBookingTourSnapshotReader>();
        var providerReader = Substitute.For<IBookingProviderSnapshotReader>();
        var uow = Substitute.For<IBookingUnitOfWork>();
        var cache = Substitute.For<HybridCache>();
        var currentUser = Substitute.For<ICurrentUser>();
        var logger = Substitute.For<ILogger<CreateAvailabilitySlotCommandHandler>>();

        var userId = Guid.NewGuid();
        var tourId = Guid.NewGuid();
        var providerId = Guid.NewGuid();

        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(userId);
        tourReader.GetByIdAsync(tourId, Arg.Any<CancellationToken>())
            .Returns(new BookingTourSnapshot(tourId, providerId, "T", "JOD", 10m, true, true, false, null, null, 12));
        providerReader.GetByIdAsync(providerId, Arg.Any<CancellationToken>())
            .Returns(new BookingProviderSnapshot(providerId, userId, "P", BookingProviderStatus.Active));
        repo.GetTourGuideIdByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Guid.NewGuid());
        repo.AnyOverlapAsync(tourId, Arg.Any<DateOnly>(), Arg.Any<TimeOnly>(), Arg.Any<TimeOnly>(), null, Arg.Any<CancellationToken>())
            .Returns(true);

        var handler = new CreateAvailabilitySlotCommandHandler(repo, tourReader, providerReader, uow, cache, currentUser, logger);
        var result = await handler.Handle(new CreateAvailabilitySlotCommand(tourId, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1), new TimeOnly(9, 0), new TimeOnly(11, 0), 8), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors.Should().ContainSingle(e => e.Code == "AvailabilitySlot.Overlap");
    }

    [Fact]
    public async Task Non_provider_returns_forbidden()
    {
        var repo = Substitute.For<IAvailabilitySlotRepository>();
        var tourReader = Substitute.For<IBookingTourSnapshotReader>();
        var providerReader = Substitute.For<IBookingProviderSnapshotReader>();
        var uow = Substitute.For<IBookingUnitOfWork>();
        var cache = Substitute.For<HybridCache>();
        var currentUser = Substitute.For<ICurrentUser>();
        var logger = Substitute.For<ILogger<CreateAvailabilitySlotCommandHandler>>();

        var tourId = Guid.NewGuid();
        var providerId = Guid.NewGuid();

        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(Guid.NewGuid());
        tourReader.GetByIdAsync(tourId, Arg.Any<CancellationToken>())
            .Returns(new BookingTourSnapshot(tourId, providerId, "T", "JOD", 10m, true, true, false, null, null, 12));
        providerReader.GetByIdAsync(providerId, Arg.Any<CancellationToken>())
            .Returns(new BookingProviderSnapshot(providerId, Guid.NewGuid(), "P", BookingProviderStatus.Active));

        var handler = new CreateAvailabilitySlotCommandHandler(repo, tourReader, providerReader, uow, cache, currentUser, logger);
        var result = await handler.Handle(new CreateAvailabilitySlotCommand(tourId, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1), new TimeOnly(9, 0), new TimeOnly(11, 0), 8), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Forbidden);
        result.Errors.Should().ContainSingle(e => e.Code == "TourBooking.OwnerMismatch");
    }

    [Fact]
    public async Task Caller_not_registered_as_tour_guide_returns_forbidden()
    {
        // BOOKING-P0-FIX-001 #3: when GetTourGuideIdByUserIdAsync returns null we MUST fail with
        // AvailabilitySlot.NotAProvider / Forbidden rather than fall back to a XOR-derived FK.
        var repo = Substitute.For<IAvailabilitySlotRepository>();
        var tourReader = Substitute.For<IBookingTourSnapshotReader>();
        var providerReader = Substitute.For<IBookingProviderSnapshotReader>();
        var uow = Substitute.For<IBookingUnitOfWork>();
        var cache = Substitute.For<HybridCache>();
        var currentUser = Substitute.For<ICurrentUser>();
        var logger = Substitute.For<ILogger<CreateAvailabilitySlotCommandHandler>>();

        var userId = Guid.NewGuid();
        var tourId = Guid.NewGuid();
        var providerId = Guid.NewGuid();

        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(userId);
        tourReader.GetByIdAsync(tourId, Arg.Any<CancellationToken>())
            .Returns(new BookingTourSnapshot(tourId, providerId, "T", "JOD", 10m, true, true, false, null, null, 12));
        providerReader.GetByIdAsync(providerId, Arg.Any<CancellationToken>())
            .Returns(new BookingProviderSnapshot(providerId, userId, "P", BookingProviderStatus.Active));
        repo.AnyOverlapAsync(tourId, Arg.Any<DateOnly>(), Arg.Any<TimeOnly>(), Arg.Any<TimeOnly>(), null, Arg.Any<CancellationToken>())
            .Returns(false);
        // Critical: tour-guide lookup returns null → handler must short-circuit with Forbidden.
        repo.GetTourGuideIdByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((Guid?)null);

        var handler = new CreateAvailabilitySlotCommandHandler(repo, tourReader, providerReader, uow, cache, currentUser, logger);
        var result = await handler.Handle(
            new CreateAvailabilitySlotCommand(tourId, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1), new TimeOnly(9, 0), new TimeOnly(11, 0), 8),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Forbidden);
        result.Errors.Should().ContainSingle(e => e.Code == "AvailabilitySlot.NotAProvider");
        await repo.DidNotReceive().AddAsync(Arg.Any<Booking.Domain.Entities.AvailabilitySlot>(), Arg.Any<CancellationToken>());
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
