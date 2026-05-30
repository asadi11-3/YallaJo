using FluentValidation;

namespace Accounts.Application.Commands.Admin.SuspendProvider;

public sealed class SuspendProviderCommandValidator : AbstractValidator<SuspendProviderCommand>
{
    public SuspendProviderCommandValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty().WithMessage("Application ID is required.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Suspension reason is required.")
            .MaximumLength(1000).WithMessage("Suspension reason must not exceed 1000 characters.");
    }
}
