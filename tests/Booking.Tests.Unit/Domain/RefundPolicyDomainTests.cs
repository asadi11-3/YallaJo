using Booking.Domain.Entities;
using Booking.Domain.ValueObjects;
using FluentAssertions;
using YallaJo.SharedKernel.Domain.Exceptions;

namespace Booking.Tests.Unit.Domain;

/// <summary>
/// Unit tests for the new tour-scoped <see cref="RefundPolicy"/> aggregate.
/// Replaces the obsolete <c>RefundPolicyTests</c> that targeted the legacy 3-field shape.
/// </summary>
public sealed class RefundPolicyDomainTests
{
    private static readonly Guid SampleTourId = Guid.Parse("11111111-2222-3333-4444-555555555555");

    private static IEnumerable<RefundTier> ValidTiers() => new[]
    {
        new RefundTier(72, 100m),
        new RefundTier(24, 50m),
        new RefundTier(0, 0m),
    };

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_with_valid_tiers_returns_aggregate_sorted_descending()
    {
        var policy = RefundPolicy.Create(SampleTourId, new[]
        {
            new RefundTier(24, 50m),
            new RefundTier(72, 100m),
            new RefundTier(0, 0m),
        });

        policy.TourId.Should().Be(SampleTourId);
        policy.IsActive.Should().BeTrue();
        policy.Tiers.Select(t => t.HoursBeforeTour).Should().ContainInOrder(72, 24, 0);
    }

    [Fact]
    public void Create_rejects_empty_TourId()
    {
        Action act = () => RefundPolicy.Create(Guid.Empty, ValidTiers());
        act.Should().Throw<BusinessRuleViolationException>()
            .WithMessage("*TourId*");
    }

    [Fact]
    public void Create_rejects_empty_tier_list()
    {
        Action act = () => RefundPolicy.Create(SampleTourId, Array.Empty<RefundTier>());
        act.Should().Throw<BusinessRuleViolationException>()
            .WithMessage("*at least one*");
    }

    [Fact]
    public void Create_rejects_more_than_max_tier_count()
    {
        var tooMany = Enumerable.Range(0, RefundPolicy.MaxTierCount + 1)
            .Select(i => new RefundTier(i, 0m));
        Action act = () => RefundPolicy.Create(SampleTourId, tooMany);
        act.Should().Throw<BusinessRuleViolationException>()
            .WithMessage("*more than*");
    }

    [Fact]
    public void Create_rejects_duplicate_HoursBeforeTour()
    {
        Action act = () => RefundPolicy.Create(SampleTourId, new[]
        {
            new RefundTier(24, 100m),
            new RefundTier(24, 50m),
        });
        act.Should().Throw<BusinessRuleViolationException>()
            .WithMessage("*unique HoursBeforeTour*");
    }

    [Fact]
    public void Create_rejects_negative_HoursBeforeTour()
    {
        Action act = () => RefundPolicy.Create(SampleTourId, new[] { new RefundTier(-1, 50m) });
        act.Should().Throw<BusinessRuleViolationException>()
            .WithMessage("*HoursBeforeTour*");
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(100.01)]
    [InlineData(200)]
    public void Create_rejects_out_of_range_RefundPercent(decimal percent)
    {
        Action act = () => RefundPolicy.Create(SampleTourId, new[] { new RefundTier(24, percent) });
        act.Should().Throw<BusinessRuleViolationException>()
            .WithMessage("*RefundPercent*");
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public void Update_replaces_tier_list_and_bumps_UpdatedAt()
    {
        var policy = RefundPolicy.Create(SampleTourId, ValidTiers());
        var beforeUpdatedAt = policy.UpdatedAt;

        policy.Update(new[]
        {
            new RefundTier(48, 80m),
            new RefundTier(0, 0m),
        });

        policy.Tiers.Should().HaveCount(2);
        policy.Tiers.Select(t => t.HoursBeforeTour).Should().ContainInOrder(48, 0);
        policy.UpdatedAt.Should().NotBe(beforeUpdatedAt);
    }

    [Fact]
    public void Update_rejects_invalid_tier_list()
    {
        var policy = RefundPolicy.Create(SampleTourId, ValidTiers());
        Action act = () => policy.Update(Array.Empty<RefundTier>());
        act.Should().Throw<BusinessRuleViolationException>();
    }

    // ── Deactivate ────────────────────────────────────────────────────────────

    [Fact]
    public void Deactivate_flips_IsActive_to_false()
    {
        var policy = RefundPolicy.Create(SampleTourId, ValidTiers());
        policy.Deactivate();
        policy.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_is_idempotent()
    {
        var policy = RefundPolicy.Create(SampleTourId, ValidTiers());
        policy.Deactivate();
        var firstUpdated = policy.UpdatedAt;
        policy.Deactivate();
        policy.IsActive.Should().BeFalse();
        policy.UpdatedAt.Should().Be(firstUpdated);
    }

    // ── CalculateRefundPercentage ─────────────────────────────────────────────

    [Fact]
    public void CalculateRefundPercentage_returns_100_above_top_tier()
    {
        var policy = RefundPolicy.Create(SampleTourId, ValidTiers());
        policy.CalculateRefundPercentage(TimeSpan.FromHours(96)).Should().Be(100m);
    }

    [Fact]
    public void CalculateRefundPercentage_returns_top_tier_at_exact_boundary()
    {
        var policy = RefundPolicy.Create(SampleTourId, ValidTiers());
        policy.CalculateRefundPercentage(TimeSpan.FromHours(72)).Should().Be(100m);
    }

    [Fact]
    public void CalculateRefundPercentage_picks_winning_threshold_between_tiers()
    {
        var policy = RefundPolicy.Create(SampleTourId, ValidTiers());
        policy.CalculateRefundPercentage(TimeSpan.FromHours(48)).Should().Be(50m);
    }

    [Fact]
    public void CalculateRefundPercentage_returns_zero_below_bottom_tier_threshold()
    {
        // tiers above all require some lead time; 0h tier exists with 0%.
        var policy = RefundPolicy.Create(SampleTourId, ValidTiers());
        policy.CalculateRefundPercentage(TimeSpan.FromHours(0.5)).Should().Be(0m);
    }

    [Fact]
    public void CalculateRefundPercentage_returns_zero_when_no_tier_matches()
    {
        // No 0h tier — only 24/72. Asking with <24h must return 0.
        var policy = RefundPolicy.Create(SampleTourId, new[]
        {
            new RefundTier(72, 100m),
            new RefundTier(24, 50m),
        });
        policy.CalculateRefundPercentage(TimeSpan.FromHours(12)).Should().Be(0m);
    }

    [Fact]
    public void CalculateRefundPercentage_returns_zero_when_time_already_passed()
    {
        var policy = RefundPolicy.Create(SampleTourId, ValidTiers());
        policy.CalculateRefundPercentage(TimeSpan.FromHours(-5)).Should().Be(0m);
    }

    // ── DefaultTiers ──────────────────────────────────────────────────────────

    [Fact]
    public void DefaultTiers_match_agreed_default_policy()
    {
        // 100% if ≥ 24h before tour, 0% otherwise.
        RefundPolicy.DefaultTiers.Should().HaveCount(2);
        RefundPolicy.DefaultTiers
            .Should().Contain(t => t.HoursBeforeTour == 24 && t.RefundPercent == 100m);
        RefundPolicy.DefaultTiers
            .Should().Contain(t => t.HoursBeforeTour == 0 && t.RefundPercent == 0m);
    }

    [Fact]
    public void Static_calculator_works_against_DefaultTiers()
    {
        RefundPolicy.CalculateRefundPercentage(RefundPolicy.DefaultTiers, TimeSpan.FromHours(48))
            .Should().Be(100m);
        RefundPolicy.CalculateRefundPercentage(RefundPolicy.DefaultTiers, TimeSpan.FromHours(12))
            .Should().Be(0m);
    }
}
