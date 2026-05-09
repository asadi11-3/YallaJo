using ContentTours.Application.Commands.TourPackage.CreateTourPackage;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Entities;
using ContentTours.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Tests.Unit;

/// <summary>
/// P1-006 regression tests for <c>CreateTourPackageCommandHandler</c>:
/// soft-deleted included tours must surface as <c>TourPackage.IncludesDeletedTour</c>
/// (was previously masked as <c>IncludesUnknownTour</c> because the soft-delete
/// query filter hid them before the deleted-check ran).
/// </summary>
public sealed class CreateTourPackageDeletedTourTests
{
    private static (
        CreateTourPackageCommandHandler Handler,
        ITourPackageRepository PackageRepo,
        ITourRepository TourRepo,
        ITourCapacityService Capacity,
        ICurrentUser CurrentUser) BuildSubject()
    {
        var packageRepo = Substitute.For<ITourPackageRepository>();
        var tourRepo = Substitute.For<ITourRepository>();
        var capacity = Substitute.For<ITourCapacityService>();
        var uow = Substitute.For<IContentToursUnitOfWork>();
        var outbox = Substitute.For<IContentToursOutboxWriter>();
        var cache = Substitute.For<HybridCache>();
        var currentUser = Substitute.For<ICurrentUser>();
        var logger = Substitute.For<ILogger<CreateTourPackageCommandHandler>>();

        var handler = new CreateTourPackageCommandHandler(
            packageRepo, tourRepo, capacity, uow, outbox, cache, currentUser, logger);
        return (handler, packageRepo, tourRepo, capacity, currentUser);
    }

    private static CreateTourPackageCommand BuildCommand(params Guid[] includedTourIds) =>
        new(
            Name:            "Two-Day Highlights",
            Description:     null,
            Price:           150m,
            Currency:        "JOD",
            MaxParticipants: null,
            ValidFrom:       null,
            ValidTo:         null,
            IncludedTourIds: includedTourIds,
            Inclusions:      Array.Empty<string>());

    [Fact]
    public async Task IncludedTourIsSoftDeleted_ReturnsIncludesDeletedTour()
    {
        var (handler, _, tourRepo, _, currentUser) = BuildSubject();
        var caller = Guid.NewGuid();

        var activeTour  = TestTourFactory.CreateApproved(createdByUserId: caller);
        var deletedTour = TestTourFactory.CreateApproved(createdByUserId: caller);
        deletedTour.SoftDelete();   // IsDeleted = true

        currentUser.UserId.Returns(caller);
        currentUser.Roles.Returns(new[] { AppRoles.User });

        // The repository's IgnoreQueryFilters path returns BOTH tours, including the
        // soft-deleted one — exactly the surface the deleted-tour branch needs.
        tourRepo.GetByIdsIncludingDeletedAsync(
                Arg.Is<IReadOnlyCollection<Guid>>(ids =>
                    ids.Contains(activeTour.Id) && ids.Contains(deletedTour.Id)),
                Arg.Any<CancellationToken>())
            .Returns(new List<Tour> { activeTour, deletedTour });

        var result = await handler.Handle(
            BuildCommand(activeTour.Id, deletedTour.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Invalid);
        result.Errors.Should().ContainSingle(e => e.Code == "TourPackage.IncludesDeletedTour");
    }

    [Fact]
    public async Task IncludedTourMissing_ReturnsIncludesUnknownTour()
    {
        var (handler, _, tourRepo, _, currentUser) = BuildSubject();
        var caller = Guid.NewGuid();

        var existingTour = TestTourFactory.CreateApproved(createdByUserId: caller);
        var missingId    = Guid.NewGuid();

        currentUser.UserId.Returns(caller);
        currentUser.Roles.Returns(new[] { AppRoles.User });

        // Only the existing tour comes back; the missing id never appears.
        tourRepo.GetByIdsIncludingDeletedAsync(
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<Tour> { existingTour });

        var result = await handler.Handle(
            BuildCommand(existingTour.Id, missingId),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Invalid);
        result.Errors.Should().ContainSingle(e => e.Code == "TourPackage.IncludesUnknownTour");
    }

    [Fact]
    public async Task AllIncludedToursActiveApprovedAndOwned_PassesInclusionGates()
    {
        var (handler, packageRepo, tourRepo, capacity, currentUser) = BuildSubject();
        var caller = Guid.NewGuid();

        var t1 = TestTourFactory.CreateApproved(createdByUserId: caller);
        var t2 = TestTourFactory.CreateApproved(createdByUserId: caller);

        currentUser.UserId.Returns(caller);
        currentUser.Roles.Returns(new[] { AppRoles.User });

        tourRepo.GetByIdsIncludingDeletedAsync(
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<Tour> { t1, t2 });

        capacity.AllHaveCapacityAsync(
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<int?>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await handler.Handle(
            BuildCommand(t1.Id, t2.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);

        // Repository was called via the IgnoreQueryFilters surface (P1-006).
        await tourRepo.Received(1).GetByIdsIncludingDeletedAsync(
            Arg.Any<IReadOnlyCollection<Guid>>(),
            Arg.Any<CancellationToken>());

        await packageRepo.Received(1).AddAsync(
            Arg.Any<TourPackage>(),
            Arg.Any<CancellationToken>());
    }
}
