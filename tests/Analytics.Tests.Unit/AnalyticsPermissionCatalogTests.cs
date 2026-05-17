using Analytics.Contracts.Authorization;
using FluentAssertions;
using Xunit;

namespace Analytics.Tests.Unit;

public class AnalyticsPermissionCatalogTests
{
    [Fact]
    public void ModuleName_ShouldBeAnalytics()
    {
        var catalog = new AnalyticsPermissionCatalog();
        catalog.ModuleName.Should().Be("Analytics");
    }

    [Fact]
    public void Permissions_ShouldNotBeEmpty()
    {
        var catalog = new AnalyticsPermissionCatalog();
        catalog.Permissions.Should().NotBeEmpty();
    }
}
