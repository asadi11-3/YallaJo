using ContentTours.Application.Commands.GuideTourOffering.DisablePrivateTour;
using ContentTours.Application.Commands.GuideTourOffering.EnablePrivateTour;
using ContentTours.Application.Commands.GuideTourOffering.RemoveGuideOffering;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Entities;
using ContentTours.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Tests.Unit.Mohammad;

/// <summary>
/// Ownership-enforcement tests for the three guide self-service offering handlers
/// (GAP-G18): RemoveGuideOffering, EnablePrivateTour, DisablePrivateTour.
/// A guide may only manage their OWN offering; a mismatched/absent caller guide
/// must yield <see cref="Outcome.Forbidden"/> with error code "GuideTourOffering.NotOwner"
/// and must NOT persist any change.
/// </summary>
public sealed class GuideOfferingOwnershipTests
{
    private const string NotOwnerCode = "GuideTourOffering.NotOwner";

    private static readonly Guid OwnerUserId = Guid.NewGuid();

    private static TourGuide BuildGuide() =>
        TourGuide.Register(
            userId: OwnerUserId,
            displayName: "Test Guide",
            slug: "test-guide",
            bio: "Experienced local guide.",
            yearsOfExperience: 3,
            hasFirstAid: true,
            moTALicenseNumber: null).Value;

    private static GuideTourOffering BuildOffering(Guid tourId, Guid tourGuideId) =>
        GuideTourOffering.Create(tourId, tourGuideId);

    private static (
        IGuideTourOfferingRepository OfferingRepo,
        ITourGuideRepository GuideRepo,
        IContentToursUnitOfWork Uow,
        HybridCache Cache,
        ICurrentUser CurrentUser) BuildDeps(
            GuideTourOffering? offering,
            TourGuide? callerGuide,
            Guid tourId,
            Guid tourGuideId)
    {
        var offeringRepo = Substitute.For<IGuideTourOfferingRepository>();
        offeringRepo
            .GetByTourAndGuideAsync(tourId, tourGuideId, Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns(offering);

        var guideRepo = Substitute.For<ITourGuideRepository>();
        guideRepo
            .GetByUserIdAsync(OwnerUserId, Arg.Any<CancellationToken>())
            .Returns(callerGuide);

        var uow = Substitute.For<IContentToursUnitOfWork>();
        var cache = Substitute.For<HybridCache>();
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(OwnerUserId);

        return (offeringRepo, guideRepo, uow, cache, currentUser);
    }

    // ---------------------------------------------------------------------
    // RemoveGuideOffering
    // ---------------------------------------------------------------------

    [Fact]
    public async Task RemoveGuideOffering_WhenCallerIsNotOwner_ReturnsForbidden_AndDoesNotPersist()
    {
        var tourId = Guid.NewGuid();
        var callerGuide = BuildGuide();
        var foreignGuideId = Guid.NewGuid();
        var offering = BuildOffering(tourId, foreignGuideId); // offering belongs to a DIFFERENT guide
        var (offeringRepo, guideRepo, uow, cache, currentUser) =
            BuildDeps(offering, callerGuide, tourId, foreignGuideId);
        var handler = new RemoveGuideOfferingCommandHandler(
            offeringRepo, guideRepo, uow, cache, currentUser,
            NullLogger<RemoveGuideOfferingCommandHandler>.Instance);

        var result = await handler.Handle(
            new RemoveGuideOfferingCommand(tourId, foreignGuideId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Forbidden);
        result.Errors.Should().ContainSingle(e => e.Code == NotOwnerCode);
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveGuideOffering_WhenCallerGuideNotFound_ReturnsForbidden()
    {
        var tourId = Guid.NewGuid();
        var tourGuideId = Guid.NewGuid();
        var offering = BuildOffering(tourId, tourGuideId);
        var (offeringRepo, guideRepo, uow, cache, currentUser) =
            BuildDeps(offering, callerGuide: null, tourId, tourGuideId);
        var handler = new RemoveGuideOfferingCommandHandler(
            offeringRepo, guideRepo, uow, cache, currentUser,
            NullLogger<RemoveGuideOfferingCommandHandler>.Instance);

        var result = await handler.Handle(
            new RemoveGuideOfferingCommand(tourId, tourGuideId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Forbidden);
        result.Errors.Should().ContainSingle(e => e.Code == NotOwnerCode);
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveGuideOffering_WhenCallerIsOwner_Succeeds_AndPersists()
    {
        var tourId = Guid.NewGuid();
        var callerGuide = BuildGuide();
        var offering = BuildOffering(tourId, callerGuide.Id); // owned by caller
        var (offeringRepo, guideRepo, uow, cache, currentUser) =
            BuildDeps(offering, callerGuide, tourId, callerGuide.Id);
        var handler = new RemoveGuideOfferingCommandHandler(
            offeringRepo, guideRepo, uow, cache, currentUser,
            NullLogger<RemoveGuideOfferingCommandHandler>.Instance);

        var result = await handler.Handle(
            new RemoveGuideOfferingCommand(tourId, callerGuide.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ---------------------------------------------------------------------
    // EnablePrivateTour
    // ---------------------------------------------------------------------

    [Fact]
    public async Task EnablePrivateTour_WhenCallerIsNotOwner_ReturnsForbidden_AndDoesNotPersist()
    {
        var tourId = Guid.NewGuid();
        var callerGuide = BuildGuide();
        var foreignGuideId = Guid.NewGuid();
        var offering = BuildOffering(tourId, foreignGuideId);
        var (offeringRepo, guideRepo, uow, cache, currentUser) =
            BuildDeps(offering, callerGuide, tourId, foreignGuideId);
        var handler = new EnablePrivateTourCommandHandler(
            offeringRepo, guideRepo, uow, cache, currentUser,
            NullLogger<EnablePrivateTourCommandHandler>.Instance);

        var result = await handler.Handle(
            new EnablePrivateTourCommand(tourId, foreignGuideId, Multiplier: 1.5m, FlatPrice: null),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Forbidden);
        result.Errors.Should().ContainSingle(e => e.Code == NotOwnerCode);
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnablePrivateTour_WhenCallerGuideNotFound_ReturnsForbidden()
    {
        var tourId = Guid.NewGuid();
        var tourGuideId = Guid.NewGuid();
        var offering = BuildOffering(tourId, tourGuideId);
        var (offeringRepo, guideRepo, uow, cache, currentUser) =
            BuildDeps(offering, callerGuide: null, tourId, tourGuideId);
        var handler = new EnablePrivateTourCommandHandler(
            offeringRepo, guideRepo, uow, cache, currentUser,
            NullLogger<EnablePrivateTourCommandHandler>.Instance);

        var result = await handler.Handle(
            new EnablePrivateTourCommand(tourId, tourGuideId, Multiplier: 1.5m, FlatPrice: null),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Forbidden);
        result.Errors.Should().ContainSingle(e => e.Code == NotOwnerCode);
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnablePrivateTour_WhenCallerIsOwner_Succeeds_AndPersists()
    {
        var tourId = Guid.NewGuid();
        var callerGuide = BuildGuide();
        var offering = BuildOffering(tourId, callerGuide.Id);
        var (offeringRepo, guideRepo, uow, cache, currentUser) =
            BuildDeps(offering, callerGuide, tourId, callerGuide.Id);
        var handler = new EnablePrivateTourCommandHandler(
            offeringRepo, guideRepo, uow, cache, currentUser,
            NullLogger<EnablePrivateTourCommandHandler>.Instance);

        var result = await handler.Handle(
            new EnablePrivateTourCommand(tourId, callerGuide.Id, Multiplier: 1.5m, FlatPrice: null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ---------------------------------------------------------------------
    // DisablePrivateTour
    // ---------------------------------------------------------------------

    [Fact]
    public async Task DisablePrivateTour_WhenCallerIsNotOwner_ReturnsForbidden_AndDoesNotPersist()
    {
        var tourId = Guid.NewGuid();
        var callerGuide = BuildGuide();
        var foreignGuideId = Guid.NewGuid();
        var offering = BuildOffering(tourId, foreignGuideId);
        var (offeringRepo, guideRepo, uow, cache, currentUser) =
            BuildDeps(offering, callerGuide, tourId, foreignGuideId);
        var handler = new DisablePrivateTourCommandHandler(
            offeringRepo, guideRepo, uow, cache, currentUser,
            NullLogger<DisablePrivateTourCommandHandler>.Instance);

        var result = await handler.Handle(
            new DisablePrivateTourCommand(tourId, foreignGuideId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Forbidden);
        result.Errors.Should().ContainSingle(e => e.Code == NotOwnerCode);
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DisablePrivateTour_WhenCallerGuideNotFound_ReturnsForbidden()
    {
        var tourId = Guid.NewGuid();
        var tourGuideId = Guid.NewGuid();
        var offering = BuildOffering(tourId, tourGuideId);
        var (offeringRepo, guideRepo, uow, cache, currentUser) =
            BuildDeps(offering, callerGuide: null, tourId, tourGuideId);
        var handler = new DisablePrivateTourCommandHandler(
            offeringRepo, guideRepo, uow, cache, currentUser,
            NullLogger<DisablePrivateTourCommandHandler>.Instance);

        var result = await handler.Handle(
            new DisablePrivateTourCommand(tourId, tourGuideId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Forbidden);
        result.Errors.Should().ContainSingle(e => e.Code == NotOwnerCode);
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DisablePrivateTour_WhenCallerIsOwner_Succeeds_AndPersists()
    {
        var tourId = Guid.NewGuid();
        var callerGuide = BuildGuide();
        var offering = BuildOffering(tourId, callerGuide.Id);
        // Enable first so Disable has an effect (still succeeds regardless).
        offering.EnablePrivateTour(1.5m, null);
        var (offeringRepo, guideRepo, uow, cache, currentUser) =
            BuildDeps(offering, callerGuide, tourId, callerGuide.Id);
        var handler = new DisablePrivateTourCommandHandler(
            offeringRepo, guideRepo, uow, cache, currentUser,
            NullLogger<DisablePrivateTourCommandHandler>.Instance);

        var result = await handler.Handle(
            new DisablePrivateTourCommand(tourId, callerGuide.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
