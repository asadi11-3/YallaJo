using Auth.Application.Commands.ResetPassword;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace Auth.Tests.Unit;

public sealed class ResetPasswordValidatorTests
{
    private readonly ResetPasswordCommandValidator _validator = new();

    [Fact]
    public void Should_Accept_Passwords_Up_To_128_Chars_Matching_Register_Policy()
    {
        var pwd128 = new string('A', 128);
        var result = _validator.TestValidate(
            new ResetPasswordCommand("user@example.com", "123456", pwd128, pwd128));

        result.ShouldNotHaveValidationErrorFor(x => x.NewPassword);
        result.ShouldNotHaveValidationErrorFor(x => x.ConfirmNewPassword);
    }

    [Fact]
    public void Should_Reject_When_Confirmation_Does_Not_Match()
    {
        var result = _validator.TestValidate(
            new ResetPasswordCommand("user@example.com", "123456", "Password1", "Password2"));

        result.ShouldHaveValidationErrorFor(x => x.ConfirmNewPassword);
    }

    [Fact]
    public void Should_Reject_Passwords_Longer_Than_128()
    {
        var pwd129 = new string('A', 129);
        var result = _validator.TestValidate(
            new ResetPasswordCommand("user@example.com", "123456", pwd129, pwd129));

        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }
}
