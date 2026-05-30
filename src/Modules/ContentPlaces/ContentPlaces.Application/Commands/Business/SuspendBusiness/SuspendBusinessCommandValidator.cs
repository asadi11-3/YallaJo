using FluentValidation;

namespace ContentPlaces.Application.Commands.Business.SuspendBusiness;

public sealed class SuspendBusinessCommandValidator : AbstractValidator<SuspendBusinessCommand>
{
    public SuspendBusinessCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("A suspension reason is required.")
            .MaximumLength(1000);
    }
}
