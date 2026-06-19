using System.Linq.Expressions;
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
        ITourGuideRepository GuideProfileRepo)
        Build()
    {
        var guideRepo = Substitute.For<ITourTourGuideRepository>();
        var guideProfileRepo = Substitute.For<ITourGuideRepository>();
        var logger = Substitute.For<ILogger<GetTourGuidesQueryHandler>>();

        var handler = new GetTourGuidesQueryHandler(guideRepo, guideProfileRepo, logger);
        return (handler, guideRepo, guideProfileRepo);
    }

    private static void StubQuery(ITourTourGuideRepository repo, params TourTourGuide[] guides)
        => repo.Query(
                Arg.Any<Expression<Func<TourTourGuide, bool>>>(),
                Arg.Any<Func<IQueryable<TourTourGuide>, IQueryable<TourTourGuide>>?>(),
                Arg.Any<bool>())
           .Returns(new TestAsyncQueryable<TourTourGuide>(guides));

    private static void StubGuides(ITourGuideRepository repo, params TourGuide[] guides)
        => repo.GetAllAsync(
                Arg.Any<Expression<Func<TourGuide, bool>>?>(),
                Arg.Any<Func<IQueryable<TourGuide>, IQueryable<TourGuide>>?>(),
                Arg.Any<Func<IQueryable<TourGuide>, IOrderedQueryable<TourGuide>>?>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
           .Returns(guides.ToList());

    private static TourGuide BuildGuide(Guid userId, string displayName, string? avatarUrl)
    {
        var guide = TourGuide.Register(
            userId: userId,
            displayName: displayName,
            slug: $"guide-{userId:N}",
            bio: "Experienced local guide.",
            yearsOfExperience: 3,
            hasFirstAid: true,
            moTALicenseNumber: null).Value;

        if (!string.IsNullOrWhiteSpace(avatarUrl))
        {
            guide.UpdateAvatar(avatarUrl);
        }

        return guide;
    }

    [Fact]
    public async Task List_OrdersPrimaryFirstThenTourGuideIdAscending()
    {
        var (handler, guideRepo, guideProfileRepo) = Build();
        var tourId = Guid.NewGuid();

        var highId = Guid.Parse("00000000-0000-0000-0000-000000000200");
        var lowId = Guid.Parse("00000000-0000-0000-0000-000000000010");
        var primaryId = Guid.Parse("00000000-0000-0000-0000-000000000300");

        var nonPrimaryHigh = TourTourGuide.Create(tourId, highId, isPrimary: false);
        var nonPrimaryLow = TourTourGuide.Create(tourId, lowId, isPrimary: false);
        var primary = TourTourGuide.Create(tourId, primaryId, isPrimary: true);

        StubQuery(guideRepo, nonPrimaryHigh, nonPrimaryLow, primary);
        StubGuides(guideProfileRepo);

        var result = await handler.Handle(new GetTourGuidesQuery(tourId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Select(x => x.TourGuideId)
            .Should().Equal(primaryId, lowId, highId);
    }

    [Fact]
    public async Task List_ProfileFound_MapsDisplayNameAndAvatarFromGuideEntity()
    {
        var (handler, guideRepo, guideProfileRepo) = Build();
        var tourId = Guid.NewGuid();
        var guideUserId = Guid.NewGuid();

        StubQuery(guideRepo, TourTourGuide.Create(tourId, guideUserId, isPrimary: true));
        StubGuides(guideProfileRepo, BuildGuide(guideUserId, "Guide One", "https://cdn/avatar.jpg"));

        var result = await handler.Handle(new GetTourGuidesQuery(tourId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value!.Single();
        dto.DisplayName.Should().Be("Guide One");
        dto.AvatarUrl.Should().Be("https://cdn/avatar.jpg");
    }

    [Fact]
    public async Task List_ProfileMissing_FallsBackToGuideIdString()
    {
        var (handler, guideRepo, guideProfileRepo) = Build();
        var tourId = Guid.NewGuid();
        var guideUserId = Guid.NewGuid();

        StubQuery(guideRepo, TourTourGuide.Create(tourId, guideUserId, isPrimary: false));
        StubGuides(guideProfileRepo);

        var result = await handler.Handle(new GetTourGuidesQuery(tourId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value!.Single();
        dto.DisplayName.Should().Be(guideUserId.ToString());
        dto.AvatarUrl.Should().BeNull();
    }

    [Fact]
    public async Task List_ProfileFoundButNullAvatar_RemainsSafe()
    {
        var (handler, guideRepo, guideProfileRepo) = Build();
        var tourId = Guid.NewGuid();
        var guideUserId = Guid.NewGuid();

        StubQuery(guideRepo, TourTourGuide.Create(tourId, guideUserId, isPrimary: true));
        StubGuides(guideProfileRepo, BuildGuide(guideUserId, "Guide One", avatarUrl: null));

        var result = await handler.Handle(new GetTourGuidesQuery(tourId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value!.Single();
        dto.DisplayName.Should().Be("Guide One");
        dto.AvatarUrl.Should().BeNull();
    }

    [Fact]
    public async Task List_BatchLoadsGuidesOnce_NoNPlusOne()
    {
        var (handler, guideRepo, guideProfileRepo) = Build();
        var tourId = Guid.NewGuid();
        var firstUserId = Guid.NewGuid();
        var secondUserId = Guid.NewGuid();

        StubQuery(
            guideRepo,
            TourTourGuide.Create(tourId, firstUserId, isPrimary: true),
            TourTourGuide.Create(tourId, secondUserId, isPrimary: false));
        StubGuides(
            guideProfileRepo,
            BuildGuide(firstUserId, "Guide One", "https://cdn/one.jpg"),
            BuildGuide(secondUserId, "Guide Two", "https://cdn/two.jpg"));

        var result = await handler.Handle(new GetTourGuidesQuery(tourId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().HaveCount(2);
        await guideProfileRepo.Received(1).GetAllAsync(
            Arg.Any<Expression<Func<TourGuide, bool>>?>(),
            Arg.Any<Func<IQueryable<TourGuide>, IQueryable<TourGuide>>?>(),
            Arg.Any<Func<IQueryable<TourGuide>, IOrderedQueryable<TourGuide>>?>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task List_EmptyAssignments_ReturnsEmptyCollection()
    {
        var (handler, guideRepo, guideProfileRepo) = Build();
        var tourId = Guid.NewGuid();

        StubQuery(guideRepo);
        StubGuides(guideProfileRepo);

        var result = await handler.Handle(new GetTourGuidesQuery(tourId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task List_WhenGuideLoadThrowsWithCanceledToken_ReturnsOutcomeCanceled()
    {
        var (handler, guideRepo, guideProfileRepo) = Build();
        var tourId = Guid.NewGuid();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        StubQuery(guideRepo, TourTourGuide.Create(tourId, Guid.NewGuid(), isPrimary: false));
        guideProfileRepo.GetAllAsync(
                Arg.Any<Expression<Func<TourGuide, bool>>?>(),
                Arg.Any<Func<IQueryable<TourGuide>, IQueryable<TourGuide>>?>(),
                Arg.Any<Func<IQueryable<TourGuide>, IOrderedQueryable<TourGuide>>?>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
           .Returns(_ => Task.FromException<List<TourGuide>>(new OperationCanceledException(cts.Token)));

        var result = await handler.Handle(new GetTourGuidesQuery(tourId), cts.Token);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Canceled);
        result.Errors.Should().ContainSingle(e => e.Code == "Request.Cancelled");
    }
}
