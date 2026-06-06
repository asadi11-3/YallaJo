using FluentAssertions;
using YallaJo.Web.Areas.Creator.Models.Audience;
using YallaJo.Web.Areas.Creator.Models.Dashboard;

namespace Web.Tests.Unit;

/// <summary>
/// Pure-mapper coverage for the Audience page: profile state → VM state, authoritative
/// follower count, and the public-safe follower summaries (Gap 3 Phase A) → anonymous
/// ordinal rows with page-aware positions and the "full page ⇒ has next" heuristic.
/// <para>CRITICAL: asserts no follower identity (user id) appears in any rendered VM field.</para>
/// </summary>
public sealed class CreatorAudienceMapperTests
{
    private static CreatorProfileMineResponse Profile(string status, int followerCount = 0) => new()
    {
        Id            = Guid.NewGuid(),
        UserId        = Guid.NewGuid(),
        Slug          = "jane-creator",
        DisplayName   = "Jane Creator",
        Status        = status,
        TrustTier     = "Trusted",
        FollowerCount = followerCount,
    };

    /// <summary>
    /// Builds <paramref name="n"/> public-safe follower summaries with server-style
    /// page-aware ordinals (Gap 3 Phase A) — no identity, just Ordinal + FollowedAt.
    /// </summary>
    private static List<FollowerSummaryResponse> Followers(int n, int page = 1, int pageSize = 20)
    {
        var start = ((page - 1) * pageSize) + 1;
        return Enumerable.Range(0, n)
            .Select(i => new FollowerSummaryResponse
            {
                Ordinal    = start + i,
                FollowedAt = DateTime.UtcNow.AddDays(-i),
            })
            .ToList();
    }

    [Fact]
    public void ToVm_NullProfile_IsNoProfileState()
    {
        var vm = CreatorAudienceMapper.ToVm(null, null, page: 1);

        vm.State.Should().Be(AudienceState.NoProfile);
    }

    [Theory]
    [InlineData("Suspended")]
    [InlineData("Deactivated")]
    public void ToVm_NonActiveProfile_IsUnavailable(string status)
    {
        var vm = CreatorAudienceMapper.ToVm(Profile(status, followerCount: 5), Followers(5), page: 1);

        vm.State.Should().Be(AudienceState.Unavailable);
        vm.StatusLabel.Should().Be(status);
        // No follower rows are produced for an unavailable profile.
        vm.Followers.Should().BeEmpty();
    }

    [Fact]
    public void ToVm_ActiveProfile_NoFollowers_IsActiveWithEmptyList()
    {
        var vm = CreatorAudienceMapper.ToVm(Profile("Active", followerCount: 0), [], page: 1);

        vm.State.Should().Be(AudienceState.Active);
        vm.FollowerCount.Should().Be(0);
        vm.HasFollowers.Should().BeFalse();
        vm.Followers.Should().BeEmpty();
        vm.Pager.HasNextPage.Should().BeFalse();
        vm.Pager.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public void ToVm_ActiveProfile_UsesAuthoritativeFollowerCountFromProfile()
    {
        // Count comes from /profile/mine, not the followers array length.
        var vm = CreatorAudienceMapper.ToVm(Profile("Active", followerCount: 37), Followers(20), page: 1, pageSize: 20);

        vm.FollowerCount.Should().Be(37);
        vm.Followers.Should().HaveCount(20);
    }

    [Fact]
    public void ToVm_FullPage_InfersHasNextPage()
    {
        var vm = CreatorAudienceMapper.ToVm(Profile("Active", 100), Followers(20), page: 1, pageSize: 20);

        vm.Pager.HasNextPage.Should().BeTrue("a full page of results implies more may exist");
    }

    [Fact]
    public void ToVm_PartialPage_DisablesNextPage()
    {
        var vm = CreatorAudienceMapper.ToVm(Profile("Active", 5), Followers(5), page: 1, pageSize: 20);

        vm.Pager.HasNextPage.Should().BeFalse("fewer than a full page means this is the last page");
    }

    [Fact]
    public void ToVm_Page2_OrdinalsArePageAware()
    {
        var vm = CreatorAudienceMapper.ToVm(Profile("Active", 100), Followers(20, page: 2, pageSize: 20), page: 2, pageSize: 20);

        vm.Pager.PageNumber.Should().Be(2);
        vm.Pager.HasPreviousPage.Should().BeTrue();
        vm.Followers.First().Position.Should().Be(21, "page 2 starts at follower #21");
        vm.Followers.First().Label.Should().Be("Follower #21");
        vm.Followers.Last().Position.Should().Be(40);
    }

    [Fact]
    public void ToVm_NullFollowers_DegradesToEmptyListButStillActive()
    {
        // A follower-fetch failure (null) must not drop the page out of Active state.
        var vm = CreatorAudienceMapper.ToVm(Profile("Active", 3), null, page: 1);

        vm.State.Should().Be(AudienceState.Active);
        vm.FollowerCount.Should().Be(3);
        vm.Followers.Should().BeEmpty();
    }

    [Fact]
    public void ToVm_NeverExposesFollowerIdentity()
    {
        var vm = CreatorAudienceMapper.ToVm(Profile("Active", 20), Followers(20), page: 1, pageSize: 20);

        // Gap 3 Phase A: the row VM carries only an ordinal label + followed-at date.
        // The row type must not expose any identity-bearing member (UserId/Name/Avatar).
        var rowProps = typeof(AudienceFollowerRowVm).GetProperties().Select(p => p.Name).ToHashSet();
        rowProps.Should().NotContain("UserId");
        rowProps.Should().NotContain("FollowerUserId");
        rowProps.Should().NotContain("DisplayName");
        rowProps.Should().NotContain("AvatarUrl");

        foreach (var row in vm.Followers)
            row.Label.Should().StartWith("Follower #");
    }
}
