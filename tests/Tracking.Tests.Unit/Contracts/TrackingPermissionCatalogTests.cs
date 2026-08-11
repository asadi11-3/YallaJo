using FluentAssertions;
using Tracking.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Authorization;

namespace Tracking.Tests.Unit.Contracts;

/// <summary>
/// Verifies that <see cref="TrackingPermissionCatalog"/> is correctly configured
/// so that the PermissionSeeder discovers and registers all Tracking permissions.
/// </summary>
public sealed class TrackingPermissionCatalogTests
{
    private readonly TrackingPermissionCatalog _catalog = new();

    [Fact]
    public void ModuleName_is_Tracking()
    {
        _catalog.ModuleName.Should().Be("Tracking");
    }

    [Fact]
    public void Catalog_exposes_exactly_5_permissions()
    {
        _catalog.Permissions.Should().HaveCount(5);
    }

    [Theory]
    [InlineData(AppAction.Create)]
    [InlineData(AppAction.Update)]
    [InlineData(AppAction.ReadOwn)]
    [InlineData(AppAction.ReadAny)]
    [InlineData(AppAction.Manage)]
    public void Catalog_contains_permission_for_TrackingSession_feature_and_expected_action(string action)
    {
        _catalog.Permissions
            .Should().Contain(p =>
                p.Feature == TrackingFeatures.TrackingSession
                && p.Action == action);
    }

    [Fact]
    public void All_permissions_have_non_empty_description()
    {
        _catalog.Permissions.Should().AllSatisfy(p =>
            p.Description.Should().NotBeNullOrWhiteSpace());
    }

    [Fact]
    public void All_permissions_target_TrackingSession_feature()
    {
        _catalog.Permissions.Should().AllSatisfy(p =>
            p.Feature.Should().Be(TrackingFeatures.TrackingSession));
    }

    [Fact]
    public void All_permissions_belong_to_BookingOperations_group()
    {
        _catalog.Permissions.Should().AllSatisfy(p =>
            p.Group.Should().Be(PermissionGroup.BookingOperations));
    }

    [Fact]
    public void Catalog_implements_IPermissionCatalog()
    {
        _catalog.Should().BeAssignableTo<IPermissionCatalog>();
    }

    [Fact]
    public void No_duplicate_feature_action_combinations()
    {
        var combinations = _catalog.Permissions
            .Select(p => (p.Feature, p.Action))
            .ToList();

        combinations.Should().OnlyHaveUniqueItems();
    }
}
