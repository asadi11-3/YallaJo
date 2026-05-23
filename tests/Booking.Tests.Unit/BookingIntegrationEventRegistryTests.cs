using Booking.Contracts.IntegrationEvents;
using FluentAssertions;
using YallaJo.SharedKernel.Infrastructure.Abstractions.Integration;

namespace Booking.Tests.Unit;

/// <summary>
/// Booking-side completeness checks for <see cref="IntegrationEventTypeRegistry"/>. The
/// SharedKernel.Tests.Unit suite already enforces global parity for the modules tracked by
/// its <c>KnownMappings</c> array, but Booking events are still being added to that list —
/// this Booking-scoped test pins the 3 TASK-3 provider-document events that were the
/// motivation for BOOKING-P1-CACHE-STANDARD-FIX-001 §1.4 so they cannot silently drift.
/// </summary>
public sealed class BookingIntegrationEventRegistryTests
{
    // (key, type) pairs that MUST be present in the production registry.
    public static IEnumerable<object[]> BookingProviderDocumentEvents()
    {
        yield return new object[]
        {
            "booking.provider-document.expiring.v1",
            typeof(ProviderDocumentExpiringIntegrationEvent),
        };
        yield return new object[]
        {
            "booking.provider-document.expired.v1",
            typeof(ProviderDocumentExpiredIntegrationEvent),
        };
        yield return new object[]
        {
            "booking.provider.suspended-doc-expired.v1",
            typeof(ProviderSuspendedDocumentExpiredIntegrationEvent),
        };
    }

    [Theory]
    [MemberData(nameof(BookingProviderDocumentEvents))]
    public void Registry_GetName_returns_expected_logical_name(string expectedKey, Type clrType)
    {
        var actual = IntegrationEventTypeRegistry.GetName(clrType);
        actual.Should().Be(expectedKey);
    }

    [Theory]
    [MemberData(nameof(BookingProviderDocumentEvents))]
    public void Registry_TryGetType_returns_expected_clr_type(string logicalName, Type expectedType)
    {
        var found = IntegrationEventTypeRegistry.TryGetType(logicalName, out var actual);

        found.Should().BeTrue();
        actual.Should().Be(expectedType);
    }

    [Theory]
    [MemberData(nameof(BookingProviderDocumentEvents))]
    public void Registry_round_trip_GetName_then_TryGetType_returns_same_type(string _, Type clrType)
    {
        var name = IntegrationEventTypeRegistry.GetName(clrType);
        var found = IntegrationEventTypeRegistry.TryGetType(name, out var roundTripped);

        found.Should().BeTrue();
        roundTripped.Should().Be(clrType);
    }
}
