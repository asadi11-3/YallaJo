using Booking.Contracts.Authorization;
using FluentAssertions;

namespace Booking.Tests.Unit;

/// <summary>
/// PW-7 scaffold: verifies the BookingPermissionCatalog has the expected breadth
/// (26 permissions across 8 features) and that every entry references a valid
/// feature key. Real sprint work fills in additional behavioural tests.
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
    public void Catalog_contains_26_permissions_across_8_features()
    {
        var catalog = new BookingPermissionCatalog();
        catalog.Permissions.Should().HaveCount(26);
        catalog.Permissions
            .Select(p => p.Feature)
            .Distinct()
            .Should()
            .HaveCount(8);
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
        };

        catalog.Permissions
            .Select(p => p.Feature)
            .Should()
            .OnlyContain(f => knownFeatures.Contains(f));
    }
}
