using FluentAssertions;
using NSubstitute;
using YallaJo.Web.Areas.Accounts.Models.Profile;
using YallaJo.Web.Services;

namespace Web.Tests.Unit;

public sealed class ProfileMapperTests
{
    private static ProfileResponse Sample(string? avatarUrl = "/uploads/avatars/a.png") => new()
    {
        UserId       = Guid.NewGuid(),
        FirstName    = "Joe",
        LastName     = "Doe",
        DisplayName  = "Joe D.",
        AvatarUrl    = avatarUrl,
        PhoneNumber  = "+10000000000",
        DateOfBirth  = new DateOnly(1990, 1, 1),
        Gender       = "Male",
        Country      = "JO",
        City         = "Amman",
        AddressLine  = "x",
        Email        = "joe@example.com",
    };

    [Fact]
    public void ToVm_ShouldResolveAvatarUrl_ToAbsolute_WhenResolverProvided()
    {
        var resolver = Substitute.For<IApiAssetUrlResolver>();
        resolver.Resolve("/uploads/avatars/a.png")
                .Returns("https://api.host/uploads/avatars/a.png");

        var vm = ProfileMapper.ToVm(Sample(), resolver);

        vm.AvatarUrl.Should().Be("https://api.host/uploads/avatars/a.png");
    }

    [Fact]
    public void ToVm_ShouldPassThroughAvatarUrl_WhenResolverIsNull()
    {
        var vm = ProfileMapper.ToVm(Sample(), assetResolver: null);

        vm.AvatarUrl.Should().Be("/uploads/avatars/a.png");
    }

    [Fact]
    public void ToVm_ShouldPopulate_Update_SubView_WithAllEditableFields()
    {
        var vm = ProfileMapper.ToVm(Sample(), assetResolver: null);

        vm.Update.FirstName.Should().Be("Joe");
        vm.Update.LastName.Should().Be("Doe");
        vm.Update.DateOfBirth.Should().Be(new DateOnly(1990, 1, 1));
        vm.Update.Gender.Should().Be(GenderOption.Male);
        vm.Update.Country.Should().Be("JO");
        vm.Update.City.Should().Be("Amman");
        vm.Update.AddressLine.Should().Be("x");
    }

    [Fact]
    public void ToVm_ShouldHandleNullAvatarGracefully()
    {
        var vm = ProfileMapper.ToVm(Sample(avatarUrl: null), assetResolver: null);

        vm.AvatarUrl.Should().BeNull();
    }
}
