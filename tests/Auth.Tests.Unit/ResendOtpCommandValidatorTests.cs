using Auth.Application.Commands.ResendOtp;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace Auth.Tests.Unit;

/// <summary>
/// Phase 2C-5 — locks in the rejected-purposes contract on
/// <c>/resend-otp</c>. Password reset is owned end-to-end by
/// <c>ForgotPasswordCommand</c> / <see cref="Auth.Domain.Entities.PasswordResetToken"/>;
/// activation is owned by <c>SendActivationEmailCommand</c> /
/// <see cref="Auth.Domain.Entities.ActivationToken"/>. Any legacy
/// client calling <c>/resend-otp</c> with those purposes must get a
/// clear validation error pointing them at the right command.
/// </summary>
public sealed class ResendOtpCommandValidatorTests
{
    private readonly ResendOtpCommandValidator _sut = new();

    private static ResendOtpCommand Command(string purpose) =>
        new(
            Email:          "user@example.com",
            Purpose:        purpose,
            RecaptchaToken: new string('a', 32));

    [Fact]
    public void Should_Accept_EmailVerification()
    {
        var result = _sut.TestValidate(Command("EmailVerification"));

        result.ShouldNotHaveValidationErrorFor(x => x.Purpose);
    }

    [Fact]
    public void Should_Reject_PasswordReset_WithMigrationHint()
    {
        // Phase 2C-5 closes the legacy write-leak: PasswordReset is
        // routed through /forgot-password, not /resend-otp.
        var result = _sut.TestValidate(Command("PasswordReset"));

        result.ShouldHaveValidationErrorFor(x => x.Purpose);
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(ResendOtpCommand.Purpose) &&
            e.ErrorMessage.Contains("/forgot-password", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("UserInvite")]
    [InlineData("Invite")]
    [InlineData("")]
    [InlineData("Unknown")]
    public void Should_Reject_AllOtherPurposes(string purpose)
    {
        var result = _sut.TestValidate(Command(purpose));

        result.ShouldHaveValidationErrorFor(x => x.Purpose);
    }
}
