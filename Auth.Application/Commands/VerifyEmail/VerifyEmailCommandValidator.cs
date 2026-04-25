using Auth.Application.Recaptcha;
using FluentValidation;

namespace Auth.Application.Commands.VerifyEmail;

public sealed class VerifyEmailCommandValidator : AbstractValidator<VerifyEmailCommand>
{
    public VerifyEmailCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);

        RuleFor(x => x.OtpCode)
            .NotEmpty()
            .Length(6)
            .Matches(@"^\d{6}$").WithMessage("OTP must be a 6-digit number.");

        RuleFor(x => x.RecaptchaToken).MustBeValidRecaptchaToken();
    }
}
