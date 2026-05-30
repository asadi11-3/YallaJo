using Auth.Application.Recaptcha;
using FluentValidation;

namespace Auth.Application.Commands.Login;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MaximumLength(128);

        // RECAPTCHA DISABLED - TODO: uncomment when re-enabling
        // RuleFor(x => x.RecaptchaToken).MustBeValidRecaptchaToken();
    }
}
