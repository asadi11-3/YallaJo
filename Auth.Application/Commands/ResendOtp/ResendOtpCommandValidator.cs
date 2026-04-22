using Auth.Application.Recaptcha;
using FluentValidation;

namespace Auth.Application.Commands.ResendOtp;

public sealed class ResendOtpCommandValidator : AbstractValidator<ResendOtpCommand>
{
    private static readonly string[] ValidPurposes = ["EmailVerification", "PasswordReset"];

    public ResendOtpCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Purpose)
            .NotEmpty()
            .Must(p => ValidPurposes.Contains(p))
            .WithMessage("Purpose must be 'EmailVerification' or 'PasswordReset'.");

        RuleFor(x => x.RecaptchaToken).MustBeValidRecaptchaToken();
    }
}
