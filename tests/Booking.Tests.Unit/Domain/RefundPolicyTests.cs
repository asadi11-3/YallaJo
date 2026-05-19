using System.Reflection;
using Booking.Domain.Entities;
using FluentAssertions;

namespace Booking.Tests.Unit.Domain;

/// <summary>
/// Unit tests for <see cref="RefundPolicy.CalculateRefundPercentage(TimeSpan)"/> tier walker.
/// Since the aggregate has a private constructor + private property setters (EF Core convention),
/// reflection is used purely for test-setup to populate the tier-threshold fields.
/// </summary>
public sealed class RefundPolicyTests
{
    private static RefundPolicy CreatePolicy(int fullRefundHours, int partialRefundHours, decimal partialRefundPercent)
    {
        var ctor = typeof(RefundPolicy).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
            binder: null, types: Type.EmptyTypes, modifiers: null);
        var policy = (RefundPolicy)ctor!.Invoke(null);
        SetPrivateProperty(policy, nameof(RefundPolicy.Name), "Default");
        SetPrivateProperty(policy, nameof(RefundPolicy.FullRefundHours), fullRefundHours);
        SetPrivateProperty(policy, nameof(RefundPolicy.PartialRefundHours), partialRefundHours);
        SetPrivateProperty(policy, nameof(RefundPolicy.PartialRefundPercent), partialRefundPercent);
        return policy;
    }

    private static void SetPrivateProperty<T>(T instance, string propertyName, object? value)
    {
        var prop = typeof(T).GetProperty(propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        prop!.SetValue(instance, value);
    }

    [Fact]
    public void Returns_100_when_time_until_tour_exceeds_FullRefundHours()
    {
        var policy = CreatePolicy(fullRefundHours: 72, partialRefundHours: 24, partialRefundPercent: 50m);

        var pct = policy.CalculateRefundPercentage(TimeSpan.FromHours(96));

        pct.Should().Be(100m);
    }

    [Fact]
    public void Returns_100_at_exact_FullRefundHours_boundary()
    {
        var policy = CreatePolicy(72, 24, 50m);

        policy.CalculateRefundPercentage(TimeSpan.FromHours(72)).Should().Be(100m);
    }

    [Fact]
    public void Returns_partial_percent_when_below_full_but_above_partial_threshold()
    {
        var policy = CreatePolicy(72, 24, 50m);

        var pct = policy.CalculateRefundPercentage(TimeSpan.FromHours(48));

        pct.Should().Be(50m);
    }

    [Fact]
    public void Returns_partial_at_exact_PartialRefundHours_boundary()
    {
        var policy = CreatePolicy(72, 24, 50m);

        policy.CalculateRefundPercentage(TimeSpan.FromHours(24)).Should().Be(50m);
    }

    [Fact]
    public void Returns_zero_when_below_PartialRefundHours()
    {
        var policy = CreatePolicy(72, 24, 50m);

        policy.CalculateRefundPercentage(TimeSpan.FromHours(12)).Should().Be(0m);
    }

    [Fact]
    public void Returns_zero_when_time_already_passed()
    {
        var policy = CreatePolicy(72, 24, 50m);

        policy.CalculateRefundPercentage(TimeSpan.FromHours(-5)).Should().Be(0m);
    }

    [Fact]
    public void Clamps_partial_percent_above_100_to_100()
    {
        var policy = CreatePolicy(72, 24, 150m);

        policy.CalculateRefundPercentage(TimeSpan.FromHours(48)).Should().Be(100m);
    }

    [Fact]
    public void Clamps_partial_percent_below_zero_to_zero()
    {
        var policy = CreatePolicy(72, 24, -25m);

        policy.CalculateRefundPercentage(TimeSpan.FromHours(48)).Should().Be(0m);
    }
}
