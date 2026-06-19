using ContentTours.Application.Queries.TourGuides.GetById;
using ContentTours.Application.Queries.TourGuides.GetBySlug;
using ContentTours.Application.Queries.TourGuides.GetByUserId;
using ContentTours.Application.Queries.TourGuides.ListPublic;
using ContentTours.Domain.Entities;
using ContentTours.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace ContentTours.Tests.Unit;

/// <summary>
/// Proves the public TourGuide read paths source DisplayName + AvatarUrl from the
/// TourGuide entity itself (TG-PUBLIC-AVATAR-B), not from the removed IProfileLookupService.
/// </summary>
public sealed class TourGuidePublicProfileReadTests
{
    private const string Avatar = "/uploads/guides/avatars/managed.png";
    private const string Display = "Petra Pro";

    private static TourGuide BuildGuide(Guid userId)
    {
        var guide = TourGuide.Register(
            userId: userId,
            displayName: Display,
            slug: "petra-pro",
            bio: "Experienced local guide.",
            yearsOfExperience: 5,
            hasFirstAid: true,
            moTALicenseNumber: null).Value;
        guide.UpdateAvatar(Avatar);
        return guide;
    }

    [Fact]
    public async Task GetBySlug_ReturnsDisplayNameAndAvatarFromGuideEntity()
    {
        var repo = Substitute.For<ITourGuideRepository>();
        var logger = NullLogger<GetTourGuideBySlugQueryHandler>.Instance;
        var guide = BuildGuide(Guid.NewGuid());
        repo.GetBySlugAsync("petra-pro", Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(guide);
        repo.CountAssignedToursAsync(guide.UserId, Arg.Any<CancellationToken>()).Returns(2);

        var handler = new GetTourGuideBySlugQueryHandler(repo, logger);

        var result = await handler.Handle(new GetTourGuideBySlugQuery("petra-pro"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.DisplayName.Should().Be(Display);
        result.Value.AvatarUrl.Should().Be(Avatar);
    }

    [Fact]
    public async Task GetById_ReturnsDisplayNameAndAvatarFromGuideEntity()
    {
        var repo = Substitute.For<ITourGuideRepository>();
        var logger = NullLogger<GetTourGuideByIdQueryHandler>.Instance;
        var guide = BuildGuide(Guid.NewGuid());
        repo.GetWithDetailsAsync(guide.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(guide);
        repo.CountAssignedToursAsync(guide.UserId, Arg.Any<CancellationToken>()).Returns(0);

        var handler = new GetTourGuideByIdQueryHandler(repo, logger);

        var result = await handler.Handle(new GetTourGuideByIdQuery(guide.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.DisplayName.Should().Be(Display);
        result.Value.AvatarUrl.Should().Be(Avatar);
    }

    [Fact]
    public async Task GetByUserId_ReturnsDisplayNameAndAvatarFromGuideEntity()
    {
        var repo = Substitute.For<ITourGuideRepository>();
        var logger = NullLogger<GetTourGuideByUserIdQueryHandler>.Instance;
        var userId = Guid.NewGuid();
        var guide = BuildGuide(userId);
        repo.GetWithDetailsByUserIdAsync(userId, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(guide);
        repo.CountAssignedToursAsync(userId, Arg.Any<CancellationToken>()).Returns(0);

        var handler = new GetTourGuideByUserIdQueryHandler(repo, logger);

        var result = await handler.Handle(new GetTourGuideByUserIdQuery(userId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.DisplayName.Should().Be(Display);
        result.Value.AvatarUrl.Should().Be(Avatar);
    }

    [Fact]
    public async Task ListPublic_ReturnsDisplayNameAndAvatarFromGuideEntity()
    {
        var repo = Substitute.For<ITourGuideRepository>();
        var logger = NullLogger<ListTourGuidesQueryHandler>.Instance;
        var guide = BuildGuide(Guid.NewGuid());
        repo.ListActiveAsync(1, 20, Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<TourGuide>)[guide], 1));
        repo.CountAssignedToursAsync(guide.UserId, Arg.Any<CancellationToken>()).Returns(0);

        var handler = new ListTourGuidesQueryHandler(repo, logger);

        var result = await handler.Handle(new ListTourGuidesQuery(1, 20), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var item = result.Value!.Items.Single();
        item.DisplayName.Should().Be(Display);
        item.AvatarUrl.Should().Be(Avatar);
        item.Slug.Should().Be("petra-pro");
    }
}
