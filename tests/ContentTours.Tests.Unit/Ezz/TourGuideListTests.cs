using System.Linq.Expressions;
using ContentTours.Application.Interfaces;
using ContentTours.Application.Queries.TourGuides;
using ContentTours.Domain.Entities;
using ContentTours.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Tests.Unit.Ezz;

public sealed class TourGuideListTests
{
    private static (
        GetTourGuidesQueryHandler Handler,
        ITourTourGuideRepository GuideRepo,
        IProfileLookupService ProfileLookup)
        Build()
    {
        var guideRepo = Substitute.For<ITourTourGuideRepository>();
        var profileLookup = Substitute.For<IProfileLookupService>();
        var logger = Substitute.For<ILogger<GetTourGuidesQueryHandler>>();

        var handler = new GetTourGuidesQueryHandler(guideRepo, profileLookup, logger);
        return (handler, guideRepo, profileLookup);
    }

    private static void StubQuery(ITourTourGuideRepository repo, params TourTourGuide[] guides)
        => repo.Query(
                Arg.Any<Expression<Func<TourTourGuide, bool>>>(),
                Arg.Any<Func<IQueryable<TourTourGuide>, IQueryable<TourTourGuide>>?>(),
                Arg.Any<bool>())
           .Returns(new TestAsyncQueryable<TourTourGuide>(guides));

    [Fact]
    public async Task List_OrdersPrimaryFirstThenTourGuideIdAscending()
    {
        var (handler, guideRepo, profileLookup) = Build();
        var tourId = Guid.NewGuid();

        var highId = Guid.Parse("00000000-0000-0000-0000-000000000200");
        var lowId = Guid.Parse("00000000-0000-0000-0000-000000000010");
        var primaryId = Guid.Parse("00000000-0000-0000-0000-000000000300");

        var nonPrimaryHigh = TourTourGuide.Create(tourId, highId, isPrimary: false);
        var nonPrimaryLow = TourTourGuide.Create(tourId, lowId, isPrimary: false);
        var primary = TourTourGuide.Create(tourId, primaryId, isPrimary: true);

        StubQuery(guideRepo, nonPrimaryHigh, nonPrimaryLow, primary);
        profileLookup.GetPublicProfileAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((PublicProfile?)null);

        var result = await handler.Handle(new GetTourGuidesQuery(tourId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Select(x => x.TourGuideId)
            .Should().Equal(primaryId, lowId, highId);
    }

    [Fact]
    public async Task List_ProfileFound_MapsDisplayNameAndAvatar()
    {
        var (handler, guideRepo, profileLookup) = Build();
        var tourId = Guid.NewGuid();
        var guideId = Guid.NewGuid();

        StubQuery(guideRepo, TourTourGuide.Create(tourId, guideId, isPrimary: true));
        profileLookup.GetPublicProfileAsync(guideId, Arg.Any<CancellationToken>())
            .Returns(new PublicProfile(guideId, "Guide One", "https://cdn/avatar.jpg"));

        var result = await handler.Handle(new GetTourGuidesQuery(tourId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value!.Single();
        dto.DisplayName.Should().Be("Guide One");
        dto.AvatarUrl.Should().Be("https://cdn/avatar.jpg");
    }

    [Fact]
    public async Task List_ProfileMissing_FallsBackToGuideIdString()
    {
        var (handler, guideRepo, profileLookup) = Build();
        var tourId = Guid.NewGuid();
        var guideId = Guid.NewGuid();

        StubQuery(guideRepo, TourTourGuide.Create(tourId, guideId, isPrimary: false));
        profileLookup.GetPublicProfileAsync(guideId, Arg.Any<CancellationToken>())
            .Returns((PublicProfile?)null);

        var result = await handler.Handle(new GetTourGuidesQuery(tourId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value!.Single();
        dto.DisplayName.Should().Be(guideId.ToString());
        dto.AvatarUrl.Should().BeNull();
    }

    [Fact]
    public async Task List_EmptyAssignments_ReturnsEmptyCollection()
    {
        var (handler, guideRepo, _) = Build();
        var tourId = Guid.NewGuid();

        StubQuery(guideRepo);

        var result = await handler.Handle(new GetTourGuidesQuery(tourId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task List_WhenProfileLookupThrowsWithCanceledToken_ReturnsOutcomeCanceled()
    {
        var (handler, guideRepo, profileLookup) = Build();
        var tourId = Guid.NewGuid();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        StubQuery(guideRepo, TourTourGuide.Create(tourId, Guid.NewGuid(), isPrimary: false));
        profileLookup.GetPublicProfileAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<PublicProfile?>(new OperationCanceledException(cts.Token)));

        var result = await handler.Handle(new GetTourGuidesQuery(tourId), cts.Token);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Canceled);
        result.Errors.Should().ContainSingle(e => e.Code == "Request.Cancelled");
    }
}
