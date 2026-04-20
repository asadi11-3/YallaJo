using FluentValidation;

namespace ContentPlaces.Application.Commands.Business.RejectBusiness;

public sealed class RejectBusinessCommandValidator : AbstractValidator<RejectBusinessCommand>
{
    public RejectBusinessCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("A rejection reason is required.")
            .MaximumLength(1000);
    }
}
