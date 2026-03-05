using FluentValidation;

namespace Auth.Application.Commands.LinkExternalProvider;

public sealed class LinkExternalProviderCommandValidator : AbstractValidator<LinkExternalProviderCommand>
{
    public LinkExternalProviderCommandValidator()
    {
        RuleFor(x => x.Provider)
            .NotEmpty().WithMessage("Provider name is required.")
            .MaximumLength(50);

        RuleFor(x => x.ProviderUserId)
            .NotEmpty().WithMessage("Provider user ID is required.")
            .MaximumLength(256);

        RuleFor(x => x.ProviderEmail)
            .EmailAddress().WithMessage("Provider email must be a valid email address.")
            .MaximumLength(320)
            .When(x => !string.IsNullOrEmpty(x.ProviderEmail));
    }
}
