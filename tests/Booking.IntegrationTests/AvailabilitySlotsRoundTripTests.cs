using System.Globalization;
using System.Reflection;
using Booking.Application.Commands.CreateAvailabilitySlot;
using Booking.Application.Interfaces;
using Booking.Application.Queries.GetAvailabilityForTour;
using Booking.Application.Queries.GetAvailabilityForTourOnDate;
using Booking.Domain.Entities;
using Booking.Domain.Repositories;
using Booking.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using NSubstitute;
using YallaJo.SharedKernel.Application.Abstractions.Context;

namespace Booking.IntegrationTests;

public sealed class AvailabilitySlotsRoundTripTests
{
    [Fact]
    public async Task Create_then_list_query_returns_grouped_slot_with_available_count()
    {
        var options = new DbContextOptionsBuilder<BookingDbContext>()
            .UseInMemoryDatabase($"booking-availability-{Guid.NewGuid()}")
            .Options;

        await using var context = new BookingDbContext(options);

        var ownerUserId = Guid.NewGuid();
        var providerId = Guid.NewGuid();
        var tourId = Guid.NewGuid();

        context.TourGuides.Add(CreateTourGuide(ownerUserId));
        await context.SaveChangesAsync();

        var repo = CreateAvailabilitySlotRepository(context);

        var tourReader = Substitute.For<IBookingTourSnapshotReader>();
        var providerReader = Substitute.For<IBookingProviderSnapshotReader>();
        var currentUser = Substitute.For<ICurrentUser>();
        var cache = Substitute.For<HybridCache>();

        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(ownerUserId);

        tourReader.GetByIdAsync(tourId, Arg.Any<CancellationToken>())
            .Returns(new BookingTourSnapshot(tourId, providerId, "Tour", "JOD", 10m, true, true, false, null, null, 20));
        providerReader.GetByIdAsync(providerId, Arg.Any<CancellationToken>())
            .Returns(new BookingProviderSnapshot(providerId, ownerUserId, "Provider", BookingProviderStatus.Active));

        var createHandler = new CreateAvailabilitySlotCommandHandler(
            repo,
            tourReader,
            providerReader,
            new TestBookingUnitOfWork(context),
            cache,
            currentUser,
            Substitute.For<ILogger<CreateAvailabilitySlotCommandHandler>>());

        var slotDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(3);
        var createResult = await createHandler.Handle(
            new CreateAvailabilitySlotCommand(tourId, slotDate, new TimeOnly(9, 0), new TimeOnly(12, 0), 12),
            CancellationToken.None);

        createResult.IsSuccess.Should().BeTrue();
        createResult.Value!.AvailableCount.Should().Be(12);

        var listHandler = new GetAvailabilityForTourQueryHandler(
            repo,
            tourReader,
            Substitute.For<ILogger<GetAvailabilityForTourQueryHandler>>());

        var listResult = await listHandler.Handle(
            new GetAvailabilityForTourQuery(tourId, null, PageSize: 20, CountTotal: true),
            CancellationToken.None);

        listResult.IsSuccess.Should().BeTrue();
        listResult.Value!.Items.Should().ContainSingle(g => g.Date == slotDate);
        listResult.Value.Items.Single().Slots.Should().ContainSingle(s => s.AvailableCount == 12);
    }

    [Fact]
    public async Task Create_then_get_by_date_query_returns_slot()
    {
        var options = new DbContextOptionsBuilder<BookingDbContext>()
            .UseInMemoryDatabase($"booking-availability-{Guid.NewGuid()}")
            .Options;

        await using var context = new BookingDbContext(options);

        var ownerUserId = Guid.NewGuid();
        var providerId = Guid.NewGuid();
        var tourId = Guid.NewGuid();

        context.TourGuides.Add(CreateTourGuide(ownerUserId));
        await context.SaveChangesAsync();

        var repo = CreateAvailabilitySlotRepository(context);

        var tourReader = Substitute.For<IBookingTourSnapshotReader>();
        var providerReader = Substitute.For<IBookingProviderSnapshotReader>();
        var currentUser = Substitute.For<ICurrentUser>();
        var cache = Substitute.For<HybridCache>();

        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(ownerUserId);

        tourReader.GetByIdAsync(tourId, Arg.Any<CancellationToken>())
            .Returns(new BookingTourSnapshot(tourId, providerId, "Tour", "JOD", 10m, true, true, false, null, null, 20));
        providerReader.GetByIdAsync(providerId, Arg.Any<CancellationToken>())
            .Returns(new BookingProviderSnapshot(providerId, ownerUserId, "Provider", BookingProviderStatus.Active));

        var createHandler = new CreateAvailabilitySlotCommandHandler(
            repo,
            tourReader,
            providerReader,
            new TestBookingUnitOfWork(context),
            cache,
            currentUser,
            Substitute.For<ILogger<CreateAvailabilitySlotCommandHandler>>());

        var slotDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(5);
        await createHandler.Handle(
            new CreateAvailabilitySlotCommand(tourId, slotDate, new TimeOnly(14, 0), new TimeOnly(16, 0), 8),
            CancellationToken.None);

        var onDateHandler = new GetAvailabilityForTourOnDateQueryHandler(
            repo,
            tourReader,
            Substitute.For<ILogger<GetAvailabilityForTourOnDateQueryHandler>>());

        var result = await onDateHandler.Handle(
            new GetAvailabilityForTourOnDateQuery(tourId, slotDate),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Slots.Should().ContainSingle(s => s.StartTime == new TimeOnly(14, 0) && s.AvailableCount == 8);
    }

    private static IAvailabilitySlotRepository CreateAvailabilitySlotRepository(BookingDbContext context)
    {
        var repositoryType = typeof(Booking.Infrastructure.DependencyInjection).Assembly
            .GetType("Booking.Infrastructure.Repositories.AvailabilitySlotRepository")
            ?? throw new InvalidOperationException("Could not locate internal AvailabilitySlotRepository type.");

        var instance = Activator.CreateInstance(
            repositoryType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: [context],
            culture: CultureInfo.InvariantCulture);

        return (IAvailabilitySlotRepository)(instance
            ?? throw new InvalidOperationException("Failed to instantiate AvailabilitySlotRepository."));
    }

    private static TourGuide CreateTourGuide(Guid userId)
    {
        var guide = (TourGuide)Activator.CreateInstance(typeof(TourGuide), nonPublic: true)!;
        SetProperty(guide, nameof(TourGuide.Id), Guid.NewGuid());
        SetProperty(guide, nameof(TourGuide.UserId), userId);
        SetProperty(guide, nameof(TourGuide.IsActive), true);
        return guide;
    }

    private static void SetProperty<TValue>(object target, string propertyName, TValue value)
    {
        var property = target.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Property '{propertyName}' not found on {target.GetType().Name}.");

        property.SetValue(target, value);
    }

    private sealed class TestBookingUnitOfWork(BookingDbContext context) : IBookingUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => context.SaveChangesAsync(cancellationToken);
    }
}
