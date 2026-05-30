using Auth.Application.Recaptcha;
using FluentValidation;

namespace Auth.Application.Commands.ResendOtp;

public sealed class ResendOtpCommandValidator : AbstractValidator<ResendOtpCommand>
{
    private static readonly string[] ValidPurposes = ["EmailVerification"];

    public ResendOtpCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Purpose)
            .NotEmpty()
            .Must(p => ValidPurposes.Contains(p))
            .WithMessage(
                "Unsupported OTP purpose. Password reset resend must be requested via /forgot-password; /resend-otp is reserved for short-lived codes such as EmailVerification.");

        // RECAPTCHA DISABLED - TODO: uncomment when re-enabling
        // RuleFor(x => x.RecaptchaToken).MustBeValidRecaptchaToken();
    }
}
