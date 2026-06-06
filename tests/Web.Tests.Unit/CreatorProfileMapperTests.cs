using FluentAssertions;
using YallaJo.Web.Areas.Creator.Models.Dashboard;
using YallaJo.Web.Areas.Creator.Models.Profile;

namespace Web.Tests.Unit;

/// <summary>
/// Pure-mapper coverage for the CCD-3 profile flow: read DTO → VM (status-driven
/// editability) and edit form → outbound request bodies (trimming, blank→null slug).
/// </summary>
public sealed class CreatorProfileMapperTests
{
    private static CreatorProfileMineResponse Profile(string status) => new()
    {
        Id            = Guid.NewGuid(),
        UserId        = Guid.NewGuid(),
        Slug          = "my-slug",
        DisplayName   = "Jane Creator",
        Bio           = "bio text",
        AvatarUrl     = "https://cdn/a.jpg",
        TrustTier     = "Trusted",
        Status        = status,
        ArticleCount  = 4,
        FollowerCount = 9,
    };

    [Fact]
    public void ToVm_NullProfile_HasNoProfile()
    {
        var vm = CreatorProfileMapper.ToVm(null);

        vm.HasProfile.Should().BeFalse();
        vm.IsActive.Should().BeFalse();
    }

    [Fact]
    public void ToVm_ActiveProfile_IsEditable_AndPrefillsFields()
    {
        var vm = CreatorProfileMapper.ToVm(Profile("Active"));

        vm.HasProfile.Should().BeTrue();
        vm.IsActive.Should().BeTrue();
        vm.DisplayName.Should().Be("Jane Creator");
        vm.Bio.Should().Be("bio text");
        vm.AvatarUrl.Should().Be("https://cdn/a.jpg");
        vm.CurrentSlug.Should().Be("my-slug");
        vm.TrustTier.Should().Be("Trusted");
        vm.ArticleCount.Should().Be(4);
        vm.FollowerCount.Should().Be(9);
        // NewSlug must start empty so an unchanged submit doesn't attempt a rename.
        vm.NewSlug.Should().BeNull();
    }

    [Theory]
    [InlineData("Suspended")]
    [InlineData("Deactivated")]
    public void ToVm_NonActiveProfile_IsNotEditable(string status)
    {
        var vm = CreatorProfileMapper.ToVm(Profile(status));

        vm.HasProfile.Should().BeTrue();
        vm.IsActive.Should().BeFalse();
        vm.Status.Should().Be(status);
    }

    [Fact]
    public void ToUpdateBody_TrimsAndSendsBlankSlugAsNull()
    {
        var form = new CreatorProfileVm
        {
            DisplayName = "  Jane  ",
            Bio         = "  hi  ",
            AvatarUrl   = "  https://cdn/a.jpg  ",
            NewSlug     = "   ", // blank → keep existing slug (null)
        };

        var body = CreatorProfileMapper.ToUpdateBody(form);

        body.DisplayName.Should().Be("Jane");
        body.Bio.Should().Be("hi");
        body.AvatarUrl.Should().Be("https://cdn/a.jpg");
        body.NewSlug.Should().BeNull("a blank slug must not trigger a rename");
    }

    [Fact]
    public void ToUpdateBody_PassesThroughExplicitSlug()
    {
        var form = new CreatorProfileVm { DisplayName = "Jane", NewSlug = "new-slug" };

        CreatorProfileMapper.ToUpdateBody(form).NewSlug.Should().Be("new-slug");
    }

    [Fact]
    public void Avatar_body_trims()
    {
        CreatorProfileMapper.ToAvatarBody(new UpdateAvatarVm { AvatarUrl = "  https://a  " })
            .AvatarUrl.Should().Be("https://a");
    }
}
