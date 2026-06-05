using FluentAssertions;
using YallaJo.Web.Areas.Creator.Models.Dashboard;

namespace Web.Tests.Unit;

/// <summary>
/// Exhaustive coverage of the CCD-1 approval-gate state-resolution table in
/// <see cref="CreatorDashboardMapper"/>. Pure function — no host required.
/// </summary>
public sealed class CreatorDashboardMapperTests
{
    private static CreatorProfileMineResponse Profile(string status, int articles = 3, long views = 100) => new()
    {
        Id          = Guid.NewGuid(),
        UserId      = Guid.NewGuid(),
        Slug        = "creator-slug",
        DisplayName = "Creator Name",
        Status      = status,
        TrustTier   = "New",
        ArticleCount       = articles,
        TotalViewCount     = views,
        TotalReactionCount = 10,
        TotalCommentCount  = 5,
        FollowerCount      = 7,
    };

    private static CreatorApplicationMineResponse Application(string status, string? adminNote = null) => new()
    {
        Id              = Guid.NewGuid(),
        ApplicantUserId = Guid.NewGuid(),
        Status          = status,
        AdminNote       = adminNote,
        CreatedAt       = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
    };

    [Fact]
    public void NoProfile_NoApplication_IsNotApplied()
    {
        var vm = CreatorDashboardMapper.ToVm(profile: null, application: null);

        vm.State.Should().Be(CreatorDashboardState.NotApplied);
        vm.IsApproved.Should().BeFalse();
        vm.ShowApplicationCta.Should().BeTrue();
    }

    [Theory]
    [InlineData("Draft", CreatorDashboardState.ApplicationDraft)]
    [InlineData("Pending", CreatorDashboardState.ApplicationPending)]
    [InlineData("Rejected", CreatorDashboardState.ApplicationRejected)]
    [InlineData("MoreInfoNeeded", CreatorDashboardState.ApplicationNeedsInfo)]
    [InlineData("Approved", CreatorDashboardState.ApplicationPending)] // approved app w/o profile → neutral pending
    [InlineData("Unknown", CreatorDashboardState.NotApplied)]
    public void NoProfile_WithApplication_ResolvesByApplicationStatus(string status, CreatorDashboardState expected)
    {
        var vm = CreatorDashboardMapper.ToVm(profile: null, application: Application(status));

        vm.State.Should().Be(expected);
        vm.IsApproved.Should().BeFalse();
    }

    [Fact]
    public void RejectedApplication_CarriesAdminNote()
    {
        var vm = CreatorDashboardMapper.ToVm(
            profile: null, application: Application("Rejected", adminNote: "Needs more samples."));

        vm.State.Should().Be(CreatorDashboardState.ApplicationRejected);
        vm.AdminNote.Should().Be("Needs more samples.");
    }

    [Fact]
    public void ActiveProfile_IsApproved_AndCarriesRealStats()
    {
        var articles = new List<CreatorDashboardArticleVm>
        {
            new() { Title = "First", Status = "Published", ViewCount = 42 },
        };

        var vm = CreatorDashboardMapper.ToVm(
            Profile("Active", articles: 9, views: 999), application: null, recentArticles: articles);

        vm.State.Should().Be(CreatorDashboardState.Approved);
        vm.IsApproved.Should().BeTrue();
        vm.ShowApplicationCta.Should().BeFalse();
        vm.ArticleCount.Should().Be(9);
        vm.TotalViewCount.Should().Be(999);
        vm.RecentArticles.Should().HaveCount(1);
    }

    [Fact]
    public void ActiveProfile_WinsOverApplicationStatus()
    {
        // An approved creator still has a (historic) application; the profile decides.
        var vm = CreatorDashboardMapper.ToVm(Profile("Active"), Application("Approved"));
        vm.State.Should().Be(CreatorDashboardState.Approved);
    }

    [Theory]
    [InlineData("Suspended", CreatorDashboardState.Suspended)]
    [InlineData("Deactivated", CreatorDashboardState.Deactivated)]
    [InlineData("Unknown", CreatorDashboardState.Suspended)] // fail-safe: never expose tools
    public void NonActiveProfile_MapsToNotice_AndHidesStats(string status, CreatorDashboardState expected)
    {
        var vm = CreatorDashboardMapper.ToVm(
            Profile(status, articles: 5, views: 500),
            application: null,
            recentArticles: [new CreatorDashboardArticleVm { Title = "x" }]);

        vm.State.Should().Be(expected);
        vm.IsApproved.Should().BeFalse();
        // Stats and the snapshot must be suppressed outside the approved state.
        vm.ArticleCount.Should().Be(0);
        vm.TotalViewCount.Should().Be(0);
        vm.RecentArticles.Should().BeEmpty();
    }
}
