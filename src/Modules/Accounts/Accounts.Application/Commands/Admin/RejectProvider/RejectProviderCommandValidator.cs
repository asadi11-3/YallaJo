using FluentValidation;

namespace Accounts.Application.Commands.Admin.RejectProvider;

public sealed class RejectProviderCommandValidator : AbstractValidator<RejectProviderCommand>
{
    public RejectProviderCommandValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty().WithMessage("Application ID is required.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Rejection reason is required.")
            .MaximumLength(1000).WithMessage("Rejection reason must not exceed 1000 characters.");
    }
}
