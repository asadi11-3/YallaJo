using Auth.Domain.Entities;
using FluentAssertions;

namespace Auth.Tests.Unit;

public sealed class OtpDomainTests
{
    [Fact]
    public void Create_ShouldDefaultToUnused_AndZeroAttempts()
    {
        var otp = Otp.Create(
            userId:          Guid.NewGuid(),
            purpose:         "EmailVerification",
            codeHash:        "hash",
            deliveryChannel: "Email",
            deliveryAddress: "user@example.com");

        otp.IsUsed.Should().BeFalse();
        otp.AttemptCount.Should().Be(0);
        otp.IsExhausted.Should().BeFalse();
        otp.IsExpired().Should().BeFalse();
        otp.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(10), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void IncrementAttempt_ShouldMarkExhaustedAtFiveAttempts()
    {
        var otp = Otp.Create(Guid.NewGuid(), "EmailVerification", "hash", "Email", "u@e.com");

        for (var i = 0; i < 4; i++) otp.IncrementAttempt();
        otp.IsExhausted.Should().BeFalse();

        otp.IncrementAttempt();
        otp.IsExhausted.Should().BeTrue();
    }

    [Fact]
    public void MarkUsed_ShouldSetIsUsedAndUsedAt()
    {
        var otp = Otp.Create(Guid.NewGuid(), "EmailVerification", "hash", "Email", "u@e.com");

        otp.MarkUsed();

        otp.IsUsed.Should().BeTrue();
        otp.UsedAt.Should().NotBeNull();
        otp.UsedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_WithCustomExpiry_ShouldHonorMinutes()
    {
        var otp = Otp.Create(Guid.NewGuid(), "Invite", "hash", "Email", "u@e.com", expiryMinutes: 60 * 24 * 3);

        otp.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(3), TimeSpan.FromSeconds(10));
    }
}
