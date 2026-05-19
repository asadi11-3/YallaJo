using Booking.Domain.Enums;
using Booking.Domain.ValueObjects;
using FluentAssertions;

namespace Booking.Tests.Unit.Domain;

/// <summary>
/// Unit tests for <see cref="BookingCancellationContext"/> record. As a value object the
/// behaviour is shallow but we confirm the three primary axis configurations
/// (user / provider / admin force-majeure) and value-equality semantics.
/// </summary>
public sealed class BookingCancellationContextTests
{
    [Fact]
    public void User_initiated_context_captures_source_and_optional_reason()
    {
        var ctx = new BookingCancellationContext(
            Source: CancellationSource.User,
            Reason: "Plans changed",
            ProviderInitiated: false,
            ForceMajeureOverride: false);

        ctx.Source.Should().Be(CancellationSource.User);
        ctx.Reason.Should().Be("Plans changed");
        ctx.ProviderInitiated.Should().BeFalse();
        ctx.ForceMajeureOverride.Should().BeFalse();
    }

    [Fact]
    public void Provider_initiated_context_sets_provider_flag()
    {
        var ctx = new BookingCancellationContext(
            Source: CancellationSource.Provider,
            Reason: "Tour cancelled due to weather warning.",
            ProviderInitiated: true,
            ForceMajeureOverride: false);

        ctx.ProviderInitiated.Should().BeTrue();
        ctx.ForceMajeureOverride.Should().BeFalse();
    }

    [Fact]
    public void Admin_force_majeure_context_sets_override_flag()
    {
        var ctx = new BookingCancellationContext(
            Source: CancellationSource.Admin,
            Reason: "Hurricane evacuation in Petra region.",
            ProviderInitiated: false,
            ForceMajeureOverride: true);

        ctx.ForceMajeureOverride.Should().BeTrue();
    }

    [Fact]
    public void Two_contexts_with_same_values_are_equal()
    {
        var a = new BookingCancellationContext(CancellationSource.User, "x", false, false);
        var b = new BookingCancellationContext(CancellationSource.User, "x", false, false);

        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Different_sources_yield_different_values()
    {
        var a = new BookingCancellationContext(CancellationSource.User, null, false, false);
        var b = new BookingCancellationContext(CancellationSource.Admin, null, false, false);

        a.Should().NotBe(b);
    }
}
