using FluentAssertions;
using YallaJo.Web.Areas.Creator.Models.Audience;
using YallaJo.Web.Areas.Creator.Models.Dashboard;

namespace Web.Tests.Unit;

/// <summary>
/// Pure-mapper coverage for the CCD-7 Audience page: profile state → VM state,
/// authoritative follower count, and the bare follower-GUID array → anonymous
/// ordinal rows with page-aware positions and the "full page ⇒ has next" heuristic.
/// <para>CRITICAL: asserts the raw follower GUIDs never appear in any rendered VM field.</para>
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

    private static List<Guid> Ids(int n) => Enumerable.Range(0, n).Select(_ => Guid.NewGuid()).ToList();

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
        var vm = CreatorAudienceMapper.ToVm(Profile(status, followerCount: 5), Ids(5), page: 1);

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
        var vm = CreatorAudienceMapper.ToVm(Profile("Active", followerCount: 37), Ids(20), page: 1, pageSize: 20);

        vm.FollowerCount.Should().Be(37);
        vm.Followers.Should().HaveCount(20);
    }

    [Fact]
    public void ToVm_FullPage_InfersHasNextPage()
    {
        var vm = CreatorAudienceMapper.ToVm(Profile("Active", 100), Ids(20), page: 1, pageSize: 20);

        vm.Pager.HasNextPage.Should().BeTrue("a full page of results implies more may exist");
    }

    [Fact]
    public void ToVm_PartialPage_DisablesNextPage()
    {
        var vm = CreatorAudienceMapper.ToVm(Profile("Active", 5), Ids(5), page: 1, pageSize: 20);

        vm.Pager.HasNextPage.Should().BeFalse("fewer than a full page means this is the last page");
    }

    [Fact]
    public void ToVm_Page2_OrdinalsArePageAware()
    {
        var vm = CreatorAudienceMapper.ToVm(Profile("Active", 100), Ids(20), page: 2, pageSize: 20);

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
    public void ToVm_NeverExposesRawFollowerGuids()
    {
        var ids = Ids(20);
        var vm = CreatorAudienceMapper.ToVm(Profile("Active", 20), ids, page: 1, pageSize: 20);

        // The only follower-derived field is the ordinal Label; no GUID may leak.
        foreach (var row in vm.Followers)
        {
            foreach (var id in ids)
                row.Label.Should().NotContain(id.ToString());
        }
    }
}
