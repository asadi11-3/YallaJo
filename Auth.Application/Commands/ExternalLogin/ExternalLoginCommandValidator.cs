using Auth.Application.Recaptcha;
using FluentValidation;

namespace Auth.Application.Commands.ExternalLogin;

public sealed class ExternalLoginCommandValidator : AbstractValidator<ExternalLoginCommand>
{
    public ExternalLoginCommandValidator()
    {
        RuleFor(x => x.Ticket)
            .NotEmpty().WithMessage("External provider ticket is required.")
            .MaximumLength(4096).WithMessage("External provider ticket is invalid.");

        RuleFor(x => x.RecaptchaToken).MustBeValidRecaptchaToken();
    }
}
