using Booking.Contracts.Authorization;
using FluentAssertions;

namespace Booking.Tests.Unit;

/// <summary>
/// Verifies the BookingPermissionCatalog has the expected breadth
/// (30 permissions across 9 features) and that every entry references a valid
/// feature key. Mohammad sprint added <see cref="BookingFeatures.AdminBookingDashboard"/>
/// (2 permissions) and <see cref="BookingFeatures.TourBooking"/> + Complete (1 permission).
/// TASK 3 added <see cref="BookingFeatures.ProviderDocument"/> + Update (1 permission).
/// Original catalog had 26 perms / 8 features.
/// </summary>
public sealed class BookingPermissionCatalogTests
{
    [Fact]
    public void Catalog_exposes_expected_module_name()
    {
        var catalog = new BookingPermissionCatalog();
        catalog.ModuleName.Should().Be("Booking");
    }

    [Fact]
    public void Catalog_contains_30_permissions_across_9_features()
    {
        var catalog = new BookingPermissionCatalog();
        catalog.Permissions.Should().HaveCount(30);
        catalog.Permissions
            .Select(p => p.Feature)
            .Distinct()
            .Should()
            .HaveCount(9);
    }

    [Fact]
    public void ProviderDocument_feature_includes_Update_action()
    {
        var catalog = new BookingPermissionCatalog();
        catalog.Permissions
            .Should()
            .Contain(p => p.Feature == BookingFeatures.ProviderDocument && p.Action == "Update",
                "TASK 3 added Update action for endpoint PUT /booking/provider/documents/{id}");
    }

    [Fact]
    public void Every_permission_references_a_known_feature_constant()
    {
        var catalog = new BookingPermissionCatalog();
        var knownFeatures = new[]
        {
            BookingFeatures.TourBooking,
            BookingFeatures.AvailabilitySlot,
            BookingFeatures.RefundPolicy,
            BookingFeatures.JoinRequest,
            BookingFeatures.ProviderDocument,
            BookingFeatures.SlotLock,
            BookingFeatures.BookingAdmin,
            BookingFeatures.BookingReports,
            BookingFeatures.AdminBookingDashboard,
        };

        catalog.Permissions
            .Select(p => p.Feature)
            .Should()
            .OnlyContain(f => knownFeatures.Contains(f));
    }

    [Fact]
    public void TourBooking_feature_includes_Complete_action()
    {
        var catalog = new BookingPermissionCatalog();
        catalog.Permissions
            .Should()
            .Contain(p => p.Feature == BookingFeatures.TourBooking && p.Action == "Complete",
                "Mohammad sprint added Complete action to TourBooking feature for endpoint POST /booking/{id}/complete");
    }

    [Fact]
    public void AdminBookingDashboard_feature_includes_Read_and_Update_actions()
    {
        var catalog = new BookingPermissionCatalog();
        catalog.Permissions
            .Where(p => p.Feature == BookingFeatures.AdminBookingDashboard)
            .Select(p => p.Action)
            .Should()
            .BeEquivalentTo(new[] { "Read", "Update" });
    }
}
