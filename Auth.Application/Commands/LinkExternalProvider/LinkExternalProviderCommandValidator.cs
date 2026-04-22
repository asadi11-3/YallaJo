using Auth.Application.Recaptcha;
using FluentValidation;

namespace Auth.Application.Commands.LinkExternalProvider;

public sealed class LinkExternalProviderCommandValidator : AbstractValidator<LinkExternalProviderCommand>
{
    public LinkExternalProviderCommandValidator()
    {
        RuleFor(x => x.Ticket)
            .NotEmpty().WithMessage("External provider ticket is required.")
            .MaximumLength(4096).WithMessage("External provider ticket is invalid.");

        RuleFor(x => x.RecaptchaToken).MustBeValidRecaptchaToken();
    }
}
