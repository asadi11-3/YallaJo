using FluentAssertions;
using Social.Contracts.Authorization;
using Xunit;

namespace Social.Tests.Unit;

public sealed class SocialPermissionCatalogTests
{
    [Fact]
    public void ModuleName_Is_Social()
    {
        var catalog = new SocialPermissionCatalog();
        catalog.ModuleName.Should().Be("Social");
    }

    [Fact]
    public void Permissions_Are_NotEmpty()
    {
        var catalog = new SocialPermissionCatalog();
        catalog.Permissions.Should().NotBeEmpty();
    }
}
