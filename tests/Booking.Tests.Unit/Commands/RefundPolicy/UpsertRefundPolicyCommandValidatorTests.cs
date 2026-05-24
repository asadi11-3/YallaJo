using Booking.Application.Commands.RefundPolicy.UpsertRefundPolicy;
using Booking.Application.Queries.GetRefundPolicyByTour;
using FluentAssertions;

namespace Booking.Tests.Unit.Commands.RefundPolicy;

public sealed class UpsertRefundPolicyCommandValidatorTests
{
    private static readonly Guid TourId = Guid.Parse("dddddddd-4444-4444-4444-444444444444");

    private static UpsertRefundPolicyCommandValidator Sut() => new();

    [Fact]
    public void Valid_command_passes()
    {
        var cmd = new UpsertRefundPolicyCommand(TourId, new[]
        {
            new RefundTierDto(72, 100m),
            new RefundTierDto(24, 50m),
            new RefundTierDto(0, 0m),
        });
        var result = Sut().Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_TourId_fails()
    {
        var cmd = new UpsertRefundPolicyCommand(Guid.Empty, new[]
        {
            new RefundTierDto(24, 100m),
        });
        var result = Sut().Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpsertRefundPolicyCommand.TourId));
    }

    [Fact]
    public void Empty_tier_list_fails()
    {
        var cmd = new UpsertRefundPolicyCommand(TourId, Array.Empty<RefundTierDto>());
        var result = Sut().Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Duplicate_HoursBeforeTour_fails()
    {
        var cmd = new UpsertRefundPolicyCommand(TourId, new[]
        {
            new RefundTierDto(24, 100m),
            new RefundTierDto(24, 50m),
        });
        var result = Sut().Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("unique"));
    }

    [Fact]
    public void Negative_hours_fails()
    {
        var cmd = new UpsertRefundPolicyCommand(TourId, new[]
        {
            new RefundTierDto(-1, 50m),
        });
        var result = Sut().Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Out_of_range_percent_fails(decimal pct)
    {
        var cmd = new UpsertRefundPolicyCommand(TourId, new[]
        {
            new RefundTierDto(24, pct),
        });
        var result = Sut().Validate(cmd);
        result.IsValid.Should().BeFalse();
    }
}
