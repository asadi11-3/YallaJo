using Auth.Application.Recaptcha;
using FluentValidation;

namespace Auth.Application.Commands.ForgotPassword;

public sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);

        // RECAPTCHA DISABLED - TODO: uncomment when re-enabling
        // RuleFor(x => x.RecaptchaToken).MustBeValidRecaptchaToken();
    }
}
